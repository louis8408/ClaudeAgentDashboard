using System.Text.Json;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Settings;

/// <summary>
/// Persists the small set of user preferences to a local JSON file under the OS per-user
/// app-data directory (research.md R6, R8).
///
/// Defaults <see cref="LaunchAtLoginEnabled"/> to false rather than the true implied by the
/// spec's "expected to launch at login" assumption: defaulting it on would silently register a
/// persistent OS autostart entry the first time this class is constructed — including during
/// development and test runs — before the user has made an informed, visible choice in Settings.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _filePath;
    private SettingsData _data;

    public JsonSettingsStore()
        : this(DefaultFilePath())
    {
    }

    public JsonSettingsStore(string filePath)
    {
        _filePath = filePath;
        _data = Load(filePath);
    }

    public bool LaunchAtLoginEnabled
    {
        get => _data.LaunchAtLoginEnabled;
        set
        {
            _data = _data with { LaunchAtLoginEnabled = value };
            Save();
        }
    }

    public bool SummaryPanelCollapsed
    {
        get => _data.SummaryPanelCollapsed;
        set
        {
            _data = _data with { SummaryPanelCollapsed = value };
            Save();
        }
    }

    public bool NotifyOnIdle
    {
        get => _data.NotifyOnIdle;
        set
        {
            _data = _data with { NotifyOnIdle = value };
            Save();
        }
    }

    public bool NotifyOnWaitingForInput
    {
        get => _data.NotifyOnWaitingForInput;
        set
        {
            _data = _data with { NotifyOnWaitingForInput = value };
            Save();
        }
    }

    public bool NotifyOnEnded
    {
        get => _data.NotifyOnEnded;
        set
        {
            _data = _data with { NotifyOnEnded = value };
            Save();
        }
    }

    public AppTheme Theme
    {
        get => _data.Theme;
        set
        {
            _data = _data with { Theme = value };
            Save();
        }
    }

    public bool MinimizeToTrayOnClose
    {
        get => _data.MinimizeToTrayOnClose;
        set
        {
            _data = _data with { MinimizeToTrayOnClose = value };
            Save();
        }
    }

    private static string DefaultFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClaudeAgentDashboard",
        "settings.json");

    private static SettingsData Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return SettingsData.Empty;
        }

        var text = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(text))
        {
            return SettingsData.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize<SettingsData>(text) ?? SettingsData.Empty;
        }
        catch (JsonException)
        {
            return SettingsData.Empty;
        }
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(_data));
    }

    // The five new properties default to true/Dark via constructor parameter defaults, not just
    // SettingsData.Empty — System.Text.Json's constructor-matching deserializer falls back to a
    // missing parameter's OWN default when a property is absent from the JSON, not default(T).
    // Without this, an existing settings.json written before these properties existed (this
    // project's own JsonSettingsStoreTests + real usage already produced such files) would
    // silently deserialize NotifyOnIdle/NotifyOnWaitingForInput/NotifyOnEnded/MinimizeToTrayOnClose
    // as false instead of their intended true default.
    private sealed record SettingsData(
        bool LaunchAtLoginEnabled,
        bool SummaryPanelCollapsed,
        bool NotifyOnIdle = true,
        bool NotifyOnWaitingForInput = true,
        bool NotifyOnEnded = true,
        AppTheme Theme = AppTheme.Dark,
        bool MinimizeToTrayOnClose = true)
    {
        public static SettingsData Empty => new(
            LaunchAtLoginEnabled: false,
            SummaryPanelCollapsed: false,
            NotifyOnIdle: true,
            NotifyOnWaitingForInput: true,
            NotifyOnEnded: true,
            Theme: AppTheme.Dark,
            MinimizeToTrayOnClose: true);
    }
}
