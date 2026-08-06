using System.Diagnostics;
using System.Runtime.Versioning;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Infrastructure.Windows;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

[SupportedOSPlatform("windows")]
public class WindowsProcessAgentWatcherTests
{
    [SkippableFact]
    public void GetCurrentSessions_Finds_Process_Already_Running_Before_Watcher_Started()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        // Spawn the process FIRST, then construct/start the watcher — proving the
        // "already running before the app started" path (FR-010), not just
        // "starts while watching".
        using var process = StartMarkerProcess();
        try
        {
            WaitForWmiVisibility(process.Id);

            using var watcher = new WindowsProcessAgentWatcher();
            var sessions = watcher.GetCurrentSessions();

            Assert.Contains(sessions, s => s.WindowReference.OwningProcessId == process.Id);
        }
        finally
        {
            TryKill(process);
        }
    }

    [SkippableFact]
    public async Task Watcher_Raises_SessionEnded_Within_The_Poll_Interval_After_Process_Exits()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        using var process = StartMarkerProcess();
        try
        {
            WaitForWmiVisibility(process.Id);

            using var watcher = new WindowsProcessAgentWatcher();
            Assert.Contains(watcher.GetCurrentSessions(), s => s.WindowReference.OwningProcessId == process.Id);

            var endedTcs = new TaskCompletionSource();
            watcher.SessionEnded += session =>
            {
                if (session.WindowReference.OwningProcessId == process.Id)
                {
                    endedTcs.TrySetResult();
                }
            };

            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);

            var completed = await Task.WhenAny(endedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.Same(endedTcs.Task, completed);

            var session = watcher.GetCurrentSessions().Single(s => s.WindowReference.OwningProcessId == process.Id);
            Assert.Equal(SessionState.Ended, session.SessionState);
        }
        finally
        {
            TryKill(process);
        }
    }

    // WindowsProcessAgentWatcher now matches on the executable's own base name (not a raw
    // command-line substring), so the marker process must actually be named claude.exe — a
    // throwaway copy of cmd.exe under that name, well away from any WindowsApps path.
    private static Process StartMarkerProcess()
    {
        var markerExePath = CreateMarkerExecutable();
        var startInfo = new ProcessStartInfo
        {
            FileName = markerExePath,
            Arguments = "/c \"echo claude-agent-test-marker & ping -n 10 127.0.0.1 >nul\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start marker process.");
    }

    private static string CreateMarkerExecutable()
    {
        var markerExePath = Path.Combine(Path.GetTempPath(), $"claude-{Guid.NewGuid():N}", "claude.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(markerExePath)!);
        File.Copy(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\cmd.exe"), markerExePath);
        return markerExePath;
    }

    private static void WaitForWmiVisibility(int processId)
    {
        // WMI's process table reflects live state, but give it a brief moment on
        // slower CI hosts before the first scan.
        Thread.Sleep(300);
        _ = processId;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Already exited between the check and the kill — fine.
        }

        TryDeleteMarkerDirectory(process.StartInfo.FileName);
    }

    private static void TryDeleteMarkerDirectory(string markerExePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(markerExePath);
            if (directory is not null && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup — a lingering handle on the just-killed exe is not worth
            // failing the test over.
        }
    }
}
