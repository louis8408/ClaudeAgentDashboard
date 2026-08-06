using System.Diagnostics;
using System.Runtime.Versioning;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;
using ClaudeAgentDashboard.Infrastructure.MacOS;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

[SupportedOSPlatform("macos")]
public class MacWindowFocuserTests
{
    [SkippableFact]
    public void Focus_Activates_A_Real_Running_Application_And_Reports_Focused()
    {
        Skip.IfNot(OperatingSystem.IsMacOS());

        using var textEdit = StartTextEdit();
        try
        {
            var pid = ResolveTextEditProcessId();

            var focuser = new MacWindowFocuser();
            var reference = new TerminalWindowReference(pid);

            var result = focuser.Focus(reference);

            Assert.Equal(FocusResult.Focused, result);
            Assert.True(reference.IsResolvable);
        }
        finally
        {
            TryQuitTextEdit();
        }
    }

    [SkippableFact]
    public void Focus_Reports_WindowNoLongerAvailable_After_Application_Quits()
    {
        Skip.IfNot(OperatingSystem.IsMacOS());

        using var textEdit = StartTextEdit();
        var pid = ResolveTextEditProcessId();
        var reference = new TerminalWindowReference(pid);

        TryQuitTextEdit();
        WaitForQuitToSettle();

        var focuser = new MacWindowFocuser();
        var result = focuser.Focus(reference);

        Assert.Equal(FocusResult.WindowNoLongerAvailable, result);
        Assert.False(reference.IsResolvable);
    }

    [SkippableFact]
    public void Focus_Returns_NotAvailable_Without_Attempting_OS_Calls_When_Already_Unresolvable()
    {
        Skip.IfNot(OperatingSystem.IsMacOS());

        var reference = new TerminalWindowReference(int.MaxValue);
        reference.MarkUnresolvable();

        var focuser = new MacWindowFocuser();
        var result = focuser.Focus(reference);

        Assert.Equal(FocusResult.WindowNoLongerAvailable, result);
    }

    private static void WaitForQuitToSettle() => Thread.Sleep(500);

    private static Process StartTextEdit() =>
        Process.Start(new ProcessStartInfo("open", "-a TextEdit -n") { UseShellExecute = false })
        ?? throw new InvalidOperationException("Failed to launch TextEdit.");

    private static int ResolveTextEditProcessId()
    {
        for (var i = 0; i < 50; i++)
        {
            using var pgrep = Process.Start(new ProcessStartInfo("pgrep", "-n TextEdit")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            })!;

            var output = pgrep.StandardOutput.ReadToEnd().Trim();
            pgrep.WaitForExit();

            if (int.TryParse(output, out var pid))
            {
                return pid;
            }

            Thread.Sleep(100);
        }

        throw new TimeoutException("TextEdit never appeared in the process table.");
    }

    private static void TryQuitTextEdit()
    {
        try
        {
            using var quit = Process.Start(new ProcessStartInfo("osascript", "-e \"tell application \\\"TextEdit\\\" to quit\"")
            {
                UseShellExecute = false,
            });
            quit?.WaitForExit(5000);
        }
        catch (InvalidOperationException)
        {
            // Already quit — fine.
        }
    }
}
