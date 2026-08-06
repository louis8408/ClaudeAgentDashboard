namespace ClaudeAgentDashboard.Domain;

/// <summary>
/// A single received Claude Code hook invocation, used to derive an
/// <see cref="AgentSession"/>'s activity state (research.md R8).
/// </summary>
public sealed class ActivitySignal
{
    public string CorrelationKey { get; }
    public HookEvent HookEvent { get; }
    public DateTimeOffset OccurredAt { get; }
    public string? SummaryText { get; }
    public string? TranscriptPath { get; }

    public ActivitySignal(
        string correlationKey, HookEvent hookEvent, DateTimeOffset occurredAt,
        string? summaryText = null, string? transcriptPath = null)
    {
        if (string.IsNullOrWhiteSpace(correlationKey))
        {
            throw new ArgumentException("Correlation key must not be empty.", nameof(correlationKey));
        }

        CorrelationKey = correlationKey;
        HookEvent = hookEvent;
        OccurredAt = occurredAt;
        SummaryText = summaryText;
        TranscriptPath = transcriptPath;
    }
}
