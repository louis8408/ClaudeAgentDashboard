using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Infrastructure.Settings;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

public class JsonSettingsStoreTests
{
    [Fact]
    public void SummaryPanelCollapsed_Defaults_To_False_When_No_File_Exists()
    {
        var path = TempSettingsPath();
        try
        {
            var store = new JsonSettingsStore(path);

            Assert.False(store.SummaryPanelCollapsed);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void SummaryPanelCollapsed_RoundTrips_Through_A_Real_File()
    {
        var path = TempSettingsPath();
        try
        {
            var writer = new JsonSettingsStore(path);
            writer.SummaryPanelCollapsed = true;

            var reader = new JsonSettingsStore(path);

            Assert.True(reader.SummaryPanelCollapsed);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NotifyOnIdle_Defaults_To_True_And_RoundTrips(bool value)
    {
        var path = TempSettingsPath();
        try
        {
            var store = new JsonSettingsStore(path);
            Assert.True(store.NotifyOnIdle);

            store.NotifyOnIdle = value;
            var reader = new JsonSettingsStore(path);

            Assert.Equal(value, reader.NotifyOnIdle);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NotifyOnWaitingForInput_Defaults_To_True_And_RoundTrips(bool value)
    {
        var path = TempSettingsPath();
        try
        {
            var store = new JsonSettingsStore(path);
            Assert.True(store.NotifyOnWaitingForInput);

            store.NotifyOnWaitingForInput = value;
            var reader = new JsonSettingsStore(path);

            Assert.Equal(value, reader.NotifyOnWaitingForInput);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NotifyOnEnded_Defaults_To_True_And_RoundTrips(bool value)
    {
        var path = TempSettingsPath();
        try
        {
            var store = new JsonSettingsStore(path);
            Assert.True(store.NotifyOnEnded);

            store.NotifyOnEnded = value;
            var reader = new JsonSettingsStore(path);

            Assert.Equal(value, reader.NotifyOnEnded);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void Theme_Defaults_To_Dark_When_No_File_Exists()
    {
        var path = TempSettingsPath();
        try
        {
            var store = new JsonSettingsStore(path);

            Assert.Equal(AppTheme.Dark, store.Theme);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void Theme_RoundTrips_Through_A_Real_File()
    {
        var path = TempSettingsPath();
        try
        {
            var writer = new JsonSettingsStore(path);
            writer.Theme = AppTheme.Light;

            var reader = new JsonSettingsStore(path);

            Assert.Equal(AppTheme.Light, reader.Theme);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void MinimizeToTrayOnClose_Defaults_To_True_When_No_File_Exists()
    {
        var path = TempSettingsPath();
        try
        {
            var store = new JsonSettingsStore(path);

            Assert.True(store.MinimizeToTrayOnClose);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void MinimizeToTrayOnClose_RoundTrips_Through_A_Real_File()
    {
        var path = TempSettingsPath();
        try
        {
            var writer = new JsonSettingsStore(path);
            writer.MinimizeToTrayOnClose = false;

            var reader = new JsonSettingsStore(path);

            Assert.False(reader.MinimizeToTrayOnClose);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void New_Properties_Default_To_True_When_Reading_A_Settings_File_Written_Before_They_Existed()
    {
        var path = TempSettingsPath();
        try
        {
            // Simulates a real settings.json from before NotifyOnIdle/NotifyOnWaitingForInput/
            // NotifyOnEnded/MinimizeToTrayOnClose existed — only the two original properties.
            File.WriteAllText(path, """{"LaunchAtLoginEnabled":false,"SummaryPanelCollapsed":true}""");

            var store = new JsonSettingsStore(path);

            Assert.True(store.NotifyOnIdle);
            Assert.True(store.NotifyOnWaitingForInput);
            Assert.True(store.NotifyOnEnded);
            Assert.True(store.MinimizeToTrayOnClose);
            Assert.Equal(AppTheme.Dark, store.Theme);
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
