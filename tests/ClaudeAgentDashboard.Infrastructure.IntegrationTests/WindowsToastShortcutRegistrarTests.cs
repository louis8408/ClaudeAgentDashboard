using System.Runtime.Versioning;
using ClaudeAgentDashboard.Infrastructure.Windows;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

// Every test points the registrar at a scratch .lnk path it creates and tears down itself —
// never touches the real Start Menu "Programs" folder, so running this suite never registers
// (or leaves behind) a real shortcut on the machine running it.
[SupportedOSPlatform("windows")]
public class WindowsToastShortcutRegistrarTests
{
    [SkippableFact]
    public void EnsureRegistered_Creates_A_Shortcut_File_At_The_Given_Path()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var path = TempShortcutPath();
        try
        {
            WindowsToastShortcutRegistrar.EnsureRegistered(path, @"C:\fake\ClaudeAgentDashboard.exe", "ClaudeAgentDashboardTest");

            Assert.True(File.Exists(path));
        }
        finally
        {
            TryDelete(path);
        }
    }

    [SkippableFact]
    public void EnsureRegistered_Sets_The_AppUserModelId_Property_Readable_Back_From_The_Real_Shortcut()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var path = TempShortcutPath();
        try
        {
            WindowsToastShortcutRegistrar.EnsureRegistered(path, @"C:\fake\ClaudeAgentDashboard.exe", "ClaudeAgentDashboardTest");

            var appUserModelId = WindowsToastShortcutRegistrar.ReadAppUserModelId(path);

            Assert.Equal("ClaudeAgentDashboardTest", appUserModelId);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [SkippableFact]
    public void EnsureRegistered_Sets_The_Icon_Location_Readable_Back_From_The_Real_Shortcut()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var path = TempShortcutPath();
        try
        {
            WindowsToastShortcutRegistrar.EnsureRegistered(
                path, @"C:\fake\ClaudeAgentDashboard.exe", "ClaudeAgentDashboardTest", @"C:\fake\icon.png");

            var iconLocation = WindowsToastShortcutRegistrar.ReadIconLocation(path);

            Assert.Equal(@"C:\fake\icon.png", iconLocation);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [SkippableFact]
    public void EnsureRegistered_Is_Idempotent_When_The_Shortcut_Already_Exists()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var path = TempShortcutPath();
        try
        {
            WindowsToastShortcutRegistrar.EnsureRegistered(path, @"C:\fake\ClaudeAgentDashboard.exe", "ClaudeAgentDashboardTest");
            var firstWriteTime = File.GetLastWriteTimeUtc(path);

            WindowsToastShortcutRegistrar.EnsureRegistered(path, @"C:\fake\ClaudeAgentDashboard.exe", "ClaudeAgentDashboardTest");

            Assert.Equal(firstWriteTime, File.GetLastWriteTimeUtc(path));
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string TempShortcutPath() =>
        Path.Combine(Path.GetTempPath(), $"claude-dashboard-toast-test-{Guid.NewGuid()}.lnk");

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
