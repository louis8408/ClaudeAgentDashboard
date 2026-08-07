using System.Text.Json;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Transcripts;

/// <summary>
/// Reads token usage from the same per-session JSONL transcript file <see cref="JsonlTranscriptReader"/>
/// reads (002-command-center-dashboard research.md R1) — a separate port/implementation
/// (Interface Segregation) since it serves a different caller (the summary panel) with
/// different data than the detail overlay's transcript display. Tolerant of a missing,
/// unreadable, or malformed file, or one with no assistant turns yet — returns null rather
/// than throwing, same contract shape as <see cref="JsonlTranscriptReader"/>.
/// </summary>
public sealed class JsonlUsageMetricsReader : IUsageMetricsReader
{
    public UsageSnapshot? TryReadLatestUsage(string transcriptPath)
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

        long totalOutputTokens = 0;
        long? latestContextWindowTokensInUse = null;

        foreach (var line in lines)
        {
            var usage = TryParseAssistantUsage(line);
            if (usage is null)
            {
                continue;
            }

            totalOutputTokens += usage.Value.OutputTokens;
            latestContextWindowTokensInUse = usage.Value.InputTokens
                + usage.Value.CacheCreationInputTokens
                + usage.Value.CacheReadInputTokens;
        }

        if (latestContextWindowTokensInUse is not { } contextWindowTokensInUse)
        {
            return null;
        }

        return new UsageSnapshot(totalOutputTokens, contextWindowTokensInUse, DateTimeOffset.UtcNow);
    }

    private static (long InputTokens, long OutputTokens, long CacheCreationInputTokens, long CacheReadInputTokens)?
        TryParseAssistantUsage(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "assistant")
            {
                return null;
            }

            if (!root.TryGetProperty("message", out var message) || !message.TryGetProperty("usage", out var usage))
            {
                return null;
            }

            return (
                ReadLong(usage, "input_tokens"),
                ReadLong(usage, "output_tokens"),
                ReadLong(usage, "cache_creation_input_tokens"),
                ReadLong(usage, "cache_read_input_tokens"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static long ReadLong(JsonElement usage, string propertyName) =>
        usage.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt64()
            : 0;
}
