namespace ClaudeAgentDashboard.Domain;

/// <summary>
/// A point-in-time reading of one Agent Session's token usage, derived from its transcript
/// (002-command-center-dashboard research.md R1).
/// </summary>
public sealed class UsageSnapshot
{
    /// <summary>
    /// Standard context window size assumed for every session (research.md R2) — not read per
    /// session/model, since neither hook payloads nor the transcript identify the model with a
    /// field stable across Claude Code versions. Informational only, not a budgeting mechanism.
    /// </summary>
    public const long DefaultContextWindowTokens = 200_000;

    public long TokensUsed { get; }
    public long ContextWindowTokensInUse { get; }
    public long ContextWindowTokensAvailable { get; }
    public DateTimeOffset ReadAt { get; }

    public UsageSnapshot(long tokensUsed, long contextWindowTokensInUse, DateTimeOffset readAt)
    {
        if (tokensUsed < 0)
        {
            throw new ArgumentException("Tokens used must not be negative.", nameof(tokensUsed));
        }

        if (contextWindowTokensInUse < 0)
        {
            throw new ArgumentException("Context window tokens in use must not be negative.", nameof(contextWindowTokensInUse));
        }

        TokensUsed = tokensUsed;
        ContextWindowTokensInUse = contextWindowTokensInUse;
        ContextWindowTokensAvailable = Math.Max(0, DefaultContextWindowTokens - contextWindowTokensInUse);
        ReadAt = readAt;
    }
}
