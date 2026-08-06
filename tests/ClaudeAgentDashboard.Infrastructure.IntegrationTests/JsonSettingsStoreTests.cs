using ClaudeAgentDashboard.Infrastructure.Settings;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

public class JsonSettingsStoreTests
{
    [Fact]
    public void LaunchAtLoginEnabled_Defaults_To_False_When_No_File_Exists()
    {
        var path = TempSettingsPath();
        try
        {
            var store = new JsonSettingsStore(path);

            Assert.False(store.LaunchAtLoginEnabled);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void LaunchAtLoginEnabled_RoundTrips_Through_A_Real_File()
    {
        var path = TempSettingsPath();
        try
        {
            var writer = new JsonSettingsStore(path);
            writer.LaunchAtLoginEnabled = true;

            var reader = new JsonSettingsStore(path);

            Assert.True(reader.LaunchAtLoginEnabled);
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string TempSettingsPath() => Path.Combine(Path.GetTempPath(), $"dashboard-settings-{Guid.NewGuid()}.json");

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
