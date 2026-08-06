using System.Diagnostics;
using System.Runtime.Versioning;
using ClaudeAgentDashboard.Infrastructure.Windows;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

[SupportedOSPlatform("windows")]
public class WindowsWorkingDirectoryResolverTests
{
    [SkippableFact]
    public void Resolve_Returns_The_Real_Working_Directory_Of_A_Spawned_Process()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var expectedDirectory = Path.GetTempPath().TrimEnd('\\');
        using var process = StartMarkerProcess(expectedDirectory);
        try
        {
            var resolved = WindowsWorkingDirectoryResolver.Resolve(process.Id);

            Assert.NotNull(resolved);
            Assert.Equal(expectedDirectory, resolved!.TrimEnd('\\'), StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            TryKill(process);
        }
    }

    [SkippableFact]
    public void Resolve_Returns_Null_For_A_NonExistent_Process_Id()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var resolved = WindowsWorkingDirectoryResolver.Resolve(int.MaxValue);

        Assert.Null(resolved);
    }

    private static Process StartMarkerProcess(string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c \"ping -n 10 127.0.0.1 >nul\"",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start marker process.");
        Thread.Sleep(300);
        return process;
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
