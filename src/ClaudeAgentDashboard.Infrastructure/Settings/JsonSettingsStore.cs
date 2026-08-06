using System.Text.Json;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Settings;

/// <summary>
/// Persists the small set of user preferences (currently: launch-at-login) to a local JSON
/// file under the OS per-user app-data directory (research.md R6).
///
/// Defaults <see cref="LaunchAtLoginEnabled"/> to false rather than the true implied by the
/// spec's "expected to launch at login" assumption: there is no onboarding/settings UI yet
/// to make that an informed, visible choice, and defaulting it on would silently register a
/// persistent OS autostart entry the first time this class is constructed — including during
/// development and test runs. Flip the default once a real settings surface exists.
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

    public string? BackgroundImagePath
    {
        get => _data.BackgroundImagePath;
        set
        {
            _data = _data with { BackgroundImagePath = value };
            Save();
        }
    }

    public CardPosition? GetCardPosition(string agentLabel) =>
        _data.CardPositions.TryGetValue(agentLabel, out var position) ? position : null;

    public void SetCardPosition(string agentLabel, CardPosition position)
    {
        var updated = new Dictionary<string, CardPosition>(_data.CardPositions) { [agentLabel] = position };
        _data = _data with { CardPositions = updated };
        Save();
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

    private sealed record SettingsData(
        bool LaunchAtLoginEnabled,
        string? BackgroundImagePath,
        Dictionary<string, CardPosition> CardPositions)
    {
        public static SettingsData Empty => new(false, null, []);
    }
}
