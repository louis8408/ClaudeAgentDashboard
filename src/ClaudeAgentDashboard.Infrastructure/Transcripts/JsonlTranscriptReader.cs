using System.Text.Json;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Transcripts;

/// <summary>
/// Reads recent entries from a Claude Code transcript file (JSONL — one JSON object per line)
/// for display in the detail overlay (FR-019, R16). Reads fresh on every call rather than
/// tailing/watching the file, matching the overlay's existing poll-based refresh cadence.
/// Tolerant of an unrecognized or evolving line shape: renders whatever text it can find and
/// falls back to a raw (truncated) line rather than dropping content it doesn't understand.
/// </summary>
public sealed class JsonlTranscriptReader : ITranscriptReader
{
    private const int MaxRawLineLength = 200;

    public IReadOnlyList<string> ReadRecentEntries(string transcriptPath, int maxEntries)
    {
        if (string.IsNullOrWhiteSpace(transcriptPath) || maxEntries <= 0)
        {
            return [];
        }

        string[] lines;
        try
        {
            if (!File.Exists(transcriptPath))
            {
                return [];
            }

            lines = File.ReadAllLines(transcriptPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        var entries = new List<string>();
        foreach (var line in lines.Reverse())
        {
            if (entries.Count >= maxEntries)
            {
                break;
            }

            var rendered = TryRender(line);
            if (rendered is not null)
            {
                entries.Add(rendered);
            }
        }

        entries.Reverse();
        return entries;
    }

    private static string? TryRender(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            var role = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
            var text = ExtractText(root);

            if (text is not null)
            {
                return string.IsNullOrEmpty(role) ? text : $"{role}: {text}";
            }
        }
        catch (JsonException)
        {
            // Fall through to the raw-line fallback below — an unrecognized/evolving
            // transcript line shape is not an error, just less nicely rendered.
        }

        return line.Length <= MaxRawLineLength ? line : line[..MaxRawLineLength] + "…";
    }

    private static string? ExtractText(JsonElement root)
    {
        if (!root.TryGetProperty("message", out var message))
        {
            return null;
        }

        if (!message.TryGetProperty("content", out var content))
        {
            return null;
        }

        return content.ValueKind switch
        {
            JsonValueKind.String => content.GetString(),
            JsonValueKind.Array => ExtractTextFromContentBlocks(content),
            _ => null,
        };
    }

    private static string? ExtractTextFromContentBlocks(JsonElement contentBlocks)
    {
        foreach (var block in contentBlocks.EnumerateArray())
        {
            if (block.ValueKind == JsonValueKind.Object
                && block.TryGetProperty("text", out var textProp)
                && textProp.ValueKind == JsonValueKind.String)
            {
                return textProp.GetString();
            }
        }

        return null;
    }
}
