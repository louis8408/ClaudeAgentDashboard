using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Infrastructure.Settings;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

public class JsonSettingsStoreTests
{
    [Fact]
    public void GetCardPosition_Returns_Null_For_A_Label_Never_Set()
    {
        var path = TempSettingsPath();
        try
        {
            var store = new JsonSettingsStore(path);

            Assert.Null(store.GetCardPosition("never-seen-label"));
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void CardPosition_RoundTrips_Through_A_Real_File()
    {
        var path = TempSettingsPath();
        try
        {
            var writer = new JsonSettingsStore(path);
            writer.SetCardPosition("claude-agent-1", new CardPosition(120.5, 340.25));

            var reader = new JsonSettingsStore(path);
            var position = reader.GetCardPosition("claude-agent-1");

            Assert.NotNull(position);
            Assert.Equal(120.5, position!.Value.X);
            Assert.Equal(340.25, position.Value.Y);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void BackgroundImagePath_Defaults_To_Null_When_No_File_Exists()
    {
        var path = TempSettingsPath();
        try
        {
            var store = new JsonSettingsStore(path);

            Assert.Null(store.BackgroundImagePath);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void BackgroundImagePath_RoundTrips_Through_A_Real_File()
    {
        var path = TempSettingsPath();
        try
        {
            var writer = new JsonSettingsStore(path);
            writer.BackgroundImagePath = @"C:\Users\test\Pictures\background.png";

            var reader = new JsonSettingsStore(path);

            Assert.Equal(@"C:\Users\test\Pictures\background.png", reader.BackgroundImagePath);
        }
        finally
        {
            TryDelete(path);
        }
    }

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
