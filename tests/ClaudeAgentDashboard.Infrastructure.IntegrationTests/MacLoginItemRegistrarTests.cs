using System.Runtime.Versioning;
using ClaudeAgentDashboard.Infrastructure.MacOS;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

// Deliberately never touches the real ~/Library/LaunchAgents: every test points the
// registrar at a scratch temp directory it creates and tears down itself.
[SupportedOSPlatform("macos")]
public class MacLoginItemRegistrarTests
{
    [SkippableFact]
    public void SetEnabled_True_Then_False_RoundTrips_Through_A_Real_Plist_File()
    {
        Skip.IfNot(OperatingSystem.IsMacOS());

        var scratchDirectory = Path.Combine(Path.GetTempPath(), $"launch-agents-{Guid.NewGuid()}");
        try
        {
            var registrar = new MacLoginItemRegistrar(scratchDirectory, "/fake/ClaudeAgentDashboard");

            Assert.False(registrar.IsEnabled());

            registrar.SetEnabled(true);
            Assert.True(registrar.IsEnabled());

            registrar.SetEnabled(false);
            Assert.False(registrar.IsEnabled());
        }
        finally
        {
            if (Directory.Exists(scratchDirectory))
            {
                Directory.Delete(scratchDirectory, recursive: true);
            }
        }
    }
}
