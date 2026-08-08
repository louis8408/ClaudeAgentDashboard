using System.Text.Json;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Transcripts;

/// <summary>
/// Reads recent conversational turns from a Claude Code transcript file (JSONL — one JSON
/// object per line) for display in the detail overlay's chat view (FR-019, R16). Reads fresh
/// on every call rather than tailing/watching the file, matching the overlay's existing
/// poll-based refresh cadence. Only "user"/"assistant" lines with extractable plain text
/// become entries — hook events, attachments, and other metadata lines are filtered out
/// rather than rendered as raw JSON noise.
/// </summary>
public sealed class JsonlTranscriptReader : ITranscriptReader
{
    public IReadOnlyList<TranscriptEntry> ReadRecentEntries(string transcriptPath, int maxEntries)
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

        var entries = new List<TranscriptEntry>();
        foreach (var line in lines.Reverse())
        {
            if (entries.Count >= maxEntries)
            {
                break;
            }

            var entry = TryParseChatTurn(line);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        entries.Reverse();
        return entries;
    }

    private static TranscriptEntry? TryParseChatTurn(string line)
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
            if (role is not ("user" or "assistant"))
            {
                return null;
            }

            var text = ExtractText(root);
            return string.IsNullOrWhiteSpace(text) ? null : new TranscriptEntry(role, text);
        }
        catch (JsonException)
        {
            return null;
        }
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
