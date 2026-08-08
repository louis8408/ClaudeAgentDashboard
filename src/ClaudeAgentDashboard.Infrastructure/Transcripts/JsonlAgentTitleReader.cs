using System.Text.Json;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Transcripts;

/// <summary>
/// Reads an agent's AI-generated session title from the same per-session JSONL transcript file
/// the other transcript-backed readers use, looking for the most recent "ai-title" line.
/// Tolerant of a missing, unreadable, or malformed file, or one with no such line yet — returns
/// null rather than throwing.
/// </summary>
public sealed class JsonlAgentTitleReader : IAgentTitleReader
{
    public string? ReadLatestTitle(string transcriptPath)
    {
        if (string.IsNullOrWhiteSpace(transcriptPath))
        {
            return null;
        }

        string[] lines;
        try
        {
            if (!File.Exists(transcriptPath))
            {
                return null;
            }

            lines = File.ReadAllLines(transcriptPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var title = TryParseAiTitle(lines[i]);
            if (title is not null)
            {
                return title;
            }
        }

        return null;
    }

    private static string? TryParseAiTitle(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "ai-title")
            {
                return null;
            }

            return root.TryGetProperty("aiTitle", out var titleProp) && titleProp.ValueKind == JsonValueKind.String
                ? titleProp.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
