using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Windows;

/// <summary>
/// Raises a real Windows toast notification without requiring MSIX packaging, by shelling
/// out to a PowerShell script that activates the WinRT toast APIs directly (research.md R2)
/// — the standard, well-documented approach for unpackaged Win32/.NET apps, since referencing
/// the WinRT toast submission APIs directly from C# would require retargeting this project to
/// a Windows-specific TFM, which would break cross-platform builds for every other project
/// that references Infrastructure (tried and reverted during implementation).
///
/// KNOWN GAP: <see cref="NotificationActivated"/> only fires when the target machine has
/// PowerShell 7 installed — the default Windows PowerShell 5.1 cannot subscribe to WinRT
/// events at all, confirmed empirically. Delivery itself (the notification appearing) works
/// regardless. Reliable click-to-activate on stock Windows requires registering a COM
/// notification activator (AppUserModelId + CLSID), which is a larger follow-up task.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsToastNotifier : INotifier
{
    private const string AppUserModelId = "ClaudeAgentDashboard";

    public event Action<Guid>? NotificationActivated;

    public async Task<bool> NotifyAttention(AgentSession session, AttentionReason reason)
    {
        ArgumentNullException.ThrowIfNull(session);

        var process = StartToastProcess(BuildShowScript(session.Label, reason));
        if (process is null)
        {
            return false;
        }

        var delivered = await WaitForDeliveryConfirmationAsync(process).ConfigureAwait(false);
        _ = MonitorActivationAsync(process, session.Id);
        return delivered;
    }

    private static Process? StartToastProcess(string script)
    {
        try
        {
            // -EncodedCommand (base64 UTF-16LE), not -Command with raw script text: PowerShell's
            // command-line parsing of a multi-line -Command argument containing embedded double
            // quotes is unreliable through Win32 argument passing. Encoding sidesteps all of that.
            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

            var startInfo = new ProcessStartInfo("powershell.exe")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-EncodedCommand");
            startInfo.ArgumentList.Add(encoded);

            return Process.Start(startInfo);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static async Task<bool> WaitForDeliveryConfirmationAsync(Process process)
    {
        // The script prints exactly one of these two markers immediately after calling
        // Show(), before it moves on to (optionally) waiting for activation.
        while (true)
        {
            var line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                return false;
            }

            if (line.StartsWith("DELIVERED:", StringComparison.Ordinal))
            {
                return line.Equals("DELIVERED:True", StringComparison.Ordinal);
            }
        }
    }

    private async Task MonitorActivationAsync(Process process, Guid sessionId)
    {
        try
        {
            string? line;
            while ((line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false)) is not null)
            {
                if (line.Equals("ACTIVATED", StringComparison.Ordinal))
                {
                    NotificationActivated?.Invoke(sessionId);
                }
            }
        }
        catch (IOException)
        {
            // Process ended / pipe closed — nothing further to monitor.
        }
        finally
        {
            process.Dispose();
        }
    }

    private static string BuildShowScript(string label, AttentionReason reason)
    {
        var message = Escape(DescribeReason(label, reason));
        var title = Escape("Claude Agent Dashboard");

        // Loads the WinRT toast types, shows the toast under a fixed AppUserModelId (no
        // shortcut/registration required on Windows 10 1809+), and reports whether the OS
        // actually has notifications enabled for that id.
        //
        // Click-to-activate is best-effort only: Windows PowerShell 5.1 (the default
        // "powershell.exe" on any Windows box without PowerShell 7 installed) refuses to
        // subscribe to WinRT events at all ("Windows PowerShell cannot subscribe to Windows
        // RT events") — confirmed empirically while implementing this. Reliable click
        // activation for an unpackaged app requires registering a COM notification
        // activator (CLSID + AppUserModelId registry entries), which is a materially larger
        // undertaking than this pass covers. The subscription attempt below is wrapped so
        // its failure never blocks showing the toast; when it does succeed (PowerShell 7
        // present), activation still works via the ACTIVATED stdout marker.
        return $$"""
            [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null
            [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] > $null
            $ErrorActionPreference = 'Stop'
            try {
                $template = '<toast><visual><binding template="ToastGeneric"><text>{{title}}</text><text>{{message}}</text></binding></visual></toast>'
                $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
                $xml.LoadXml($template)
                $toast = New-Object Windows.UI.Notifications.ToastNotification $xml
                $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('{{AppUserModelId}}')
                $delivered = $notifier.Setting.ToString() -eq 'Enabled'
                $activated = $false
                $canWatchActivation = $true
                try {
                    Register-ObjectEvent -InputObject $toast -EventName Activated -Action { $script:activated = $true } | Out-Null
                } catch {
                    $canWatchActivation = $false
                }
                $notifier.Show($toast)
                Write-Output "DELIVERED:$delivered"
                if ($canWatchActivation) {
                    $deadline = (Get-Date).AddSeconds(30)
                    while (-not $activated -and (Get-Date) -lt $deadline) {
                        Start-Sleep -Milliseconds 250
                    }
                    if ($activated) {
                        Write-Output "ACTIVATED"
                    }
                }
            } catch {
                Write-Output "DELIVERED:False"
            }
            """;
    }

    private static string DescribeReason(string label, AttentionReason reason) => reason switch
    {
        AttentionReason.Idle => $"'{label}' is idle and waiting for your next instruction.",
        AttentionReason.WaitingForInput => $"'{label}' needs your input.",
        AttentionReason.Ended => $"'{label}' has ended.",
        _ => $"'{label}' needs your attention.",
    };

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
