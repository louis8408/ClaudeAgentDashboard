using System.Runtime.Versioning;
using ClaudeAgentDashboard.Infrastructure.Windows;
using Microsoft.Win32;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

// Deliberately never touches the real "CurrentVersion\Run" autostart key: every test points
// the registrar at a scratch subkey it creates and tears down itself, so running this suite
// never registers (or de-registers) real autostart behavior on the machine running it.
[SupportedOSPlatform("windows")]
public class WindowsLoginItemRegistrarTests
{
    [SkippableFact]
    public void SetEnabled_True_Then_False_RoundTrips_Through_The_Real_Registry()
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var scratchKeyPath = $"Software\\ClaudeAgentDashboardTests\\{Guid.NewGuid()}";
        try
        {
            var registrar = new WindowsLoginItemRegistrar(scratchKeyPath, "C:\\fake\\ClaudeAgentDashboard.exe");

            Assert.False(registrar.IsEnabled());

            registrar.SetEnabled(true);
            Assert.True(registrar.IsEnabled());

            registrar.SetEnabled(false);
            Assert.False(registrar.IsEnabled());
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(scratchKeyPath, throwOnMissingSubKey: false);
        }
    }
}
