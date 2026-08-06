using System.Diagnostics;
using System.Runtime.Versioning;
using ClaudeAgentDashboard.Infrastructure.MacOS;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

[SupportedOSPlatform("macos")]
public class MacProcessAgentWatcherTests
{
    [SkippableFact]
    public void GetCurrentSessions_Finds_Process_Already_Running_Before_Watcher_Started()
    {
        Skip.IfNot(OperatingSystem.IsMacOS());

        // Spawn the process FIRST, then construct/start the watcher — proving the
        // "already running before the app started" path (FR-010), not just
        // "starts while watching".
        using var process = StartMarkerProcess();
        try
        {
            WaitForProcessTableVisibility();

            using var watcher = new MacProcessAgentWatcher();
            var sessions = watcher.GetCurrentSessions();

            Assert.Contains(sessions, s => s.WindowReference.OwningProcessId == process.Id);
        }
        finally
        {
            TryKill(process);
        }
    }

    private static void WaitForProcessTableVisibility() => Thread.Sleep(300);

    private static Process StartMarkerProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/sh",
            Arguments = "-c \"echo claude-agent-test-marker; sleep 10\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start marker process.");
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
    }
}
