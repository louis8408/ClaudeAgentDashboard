using System.Text.Json;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Transcripts;

/// <summary>
/// Reads an agent's current permission mode from the same per-session JSONL transcript file
/// the other transcript-backed readers use, looking for the most recent "permission-mode"
/// line. Tolerant of a missing, unreadable, or malformed file, or one with no such line yet —
/// returns <see cref="PermissionMode.Unknown"/> rather than throwing.
/// </summary>
public sealed class JsonlPermissionModeReader : IPermissionModeReader
{
    public PermissionMode ReadLatestPermissionMode(string transcriptPath)
    {
        if (string.IsNullOrWhiteSpace(transcriptPath))
        {
            return PermissionMode.Unknown;
        }

        string[] lines;
        try
        {
            if (!File.Exists(transcriptPath))
            {
                return PermissionMode.Unknown;
            }

            lines = File.ReadAllLines(transcriptPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return PermissionMode.Unknown;
        }

        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var mode = TryParsePermissionMode(lines[i]);
            if (mode is not null)
            {
                return mode.Value;
            }
        }

        return PermissionMode.Unknown;
    }

    private static PermissionMode? TryParsePermissionMode(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "permission-mode")
            {
                return null;
            }

            if (!root.TryGetProperty("permissionMode", out var modeProp) || modeProp.ValueKind != JsonValueKind.String)
            {
                return PermissionMode.Unknown;
            }

            return modeProp.GetString() switch
            {
                "default" => PermissionMode.Manual,
                "acceptEdits" => PermissionMode.AcceptEdits,
                "plan" => PermissionMode.Plan,
                "auto" => PermissionMode.Auto,
                _ => PermissionMode.Unknown,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
