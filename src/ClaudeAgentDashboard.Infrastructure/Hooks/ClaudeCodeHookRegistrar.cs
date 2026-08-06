using System.Text.Json;
using System.Text.Json.Nodes;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Hooks;

/// <summary>
/// Installs the hook commands <see cref="HookEventListener"/> depends on into the user's
/// Claude Code configuration (FR-013's one-time setup step). Merges into the existing
/// settings file rather than overwriting it, and never touches hook entries it didn't create.
/// </summary>
public sealed class ClaudeCodeHookRegistrar : IHookRegistrar
{
    private static readonly (string HookEventName, string Route)[] RouteMap =
    [
        ("UserPromptSubmit", "user-prompt-submit"),
        ("PreToolUse", "pre-tool-use"),
        ("Stop", "stop"),
        ("Notification", "notification"),
        ("SessionEnd", "session-end"),
    ];

    // Every command this class writes contains this literal substring, letting
    // AreHooksRegistered/RegisterHooks recognize (and only touch) their own entries.
    private const string OwnershipMarker = "/hooks/";

    private readonly string _settingsFilePath;

    public ClaudeCodeHookRegistrar()
        : this(DefaultSettingsFilePath())
    {
    }

    public ClaudeCodeHookRegistrar(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
    }

    public bool AreHooksRegistered()
    {
        var hooks = LoadHooksObject();
        return Array.TrueForAll(RouteMap, entry => ContainsOwnCommand(hooks, entry.HookEventName));
    }

    public void RegisterHooks(Uri listenerBaseAddress)
    {
        ArgumentNullException.ThrowIfNull(listenerBaseAddress);

        var root = LoadRoot();
        var hooks = root["hooks"] as JsonObject ?? new JsonObject();
        root["hooks"] = hooks;

        foreach (var (hookEventName, route) in RouteMap)
        {
            Upsert(hooks, hookEventName, BuildCommand(listenerBaseAddress, route));
        }

        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_settingsFilePath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string DefaultSettingsFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "settings.json");

    private static string BuildCommand(Uri baseAddress, string route)
    {
        var url = new Uri(baseAddress, $"hooks/{route}");
        return OperatingSystem.IsWindows()
            ? $"powershell -NoProfile -Command \"$input = [Console]::In.ReadToEnd(); Invoke-RestMethod -Uri '{url}' -Method Post -Body $input -ContentType 'application/json'\""
            : $"curl -s -X POST '{url}' -H 'Content-Type: application/json' -d @-";
    }

    private JsonObject LoadRoot()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new JsonObject();
        }

        var text = File.ReadAllText(_settingsFilePath);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new JsonObject();
        }

        return JsonNode.Parse(text) as JsonObject ?? new JsonObject();
    }

    private JsonObject LoadHooksObject() => LoadRoot()["hooks"] as JsonObject ?? new JsonObject();

    private static bool ContainsOwnCommand(JsonObject hooks, string hookEventName) =>
        EnumerateCommands(hooks, hookEventName).Any(command => command.Contains(OwnershipMarker, StringComparison.Ordinal));

    private static void Upsert(JsonObject hooks, string hookEventName, string command)
    {
        var entries = hooks[hookEventName] as JsonArray;
        if (entries is null)
        {
            entries = [];
            hooks[hookEventName] = entries;
        }

        // Idempotent replace: drop only entries this registrar previously created, leave everything else as-is.
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i] is JsonObject entry && IsOwnEntry(entry))
            {
                entries.RemoveAt(i);
            }
        }

        entries.Add(new JsonObject
        {
            ["hooks"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "command",
                    ["command"] = command,
                },
            },
        });
    }

    private static bool IsOwnEntry(JsonObject entry) =>
        (entry["hooks"] as JsonArray)?
            .OfType<JsonObject>()
            .Select(h => h["command"]?.GetValue<string>())
            .Any(command => command?.Contains(OwnershipMarker, StringComparison.Ordinal) == true)
        ?? false;

    private static IEnumerable<string> EnumerateCommands(JsonObject hooks, string hookEventName)
    {
        if (hooks[hookEventName] is not JsonArray entries)
        {
            yield break;
        }

        foreach (var entry in entries.OfType<JsonObject>())
        {
            if (entry["hooks"] is not JsonArray innerHooks)
            {
                continue;
            }

            var commands = innerHooks.OfType<JsonObject>()
                .Select(h => h["command"]?.GetValue<string>())
                .Where(command => command is not null);

            foreach (var command in commands)
            {
                yield return command!;
            }
        }
    }
}
