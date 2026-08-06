namespace ClaudeAgentDashboard.Domain;

/// <summary>
/// A single detected Claude Code CLI run on the local machine (spec Key Entities).
/// </summary>
public sealed class AgentSession
{
    public Guid Id { get; }
    public string Label { get; }
    public string? WorkingDirectory { get; }
    public SessionState SessionState { get; private set; }
    public ActivityState ActivityState { get; private set; }
    public string? ActivitySummary { get; private set; }
    public string? TranscriptPath { get; private set; }
    public DateTimeOffset? ActivityChangedAt { get; private set; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? EndedAt { get; private set; }
    public TerminalWindowReference WindowReference { get; }

    public AgentSession(
        Guid id, string label, DateTimeOffset startedAt, TerminalWindowReference windowReference,
        string? workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Label must not be empty.", nameof(label));
        }

        Id = id;
        Label = label;
        WorkingDirectory = workingDirectory;
        StartedAt = startedAt;
        WindowReference = windowReference ?? throw new ArgumentNullException(nameof(windowReference));
        SessionState = SessionState.Running;
        ActivityState = ActivityState.Unknown;
    }

    /// <summary>
    /// Ends the session. One-way and idempotent: a session is never reopened (spec Assumptions),
    /// and a second call is a no-op rather than overwriting the original EndedAt.
    /// </summary>
    public void End(DateTimeOffset endedAt)
    {
        if (SessionState == SessionState.Ended)
        {
            return;
        }

        SessionState = SessionState.Ended;
        EndedAt = endedAt;
    }

    /// <summary>
    /// Folds an incoming hook-derived <see cref="ActivitySignal"/> into this session's state
    /// (research.md R8). Returns false without effect if the session has already ended, or if
    /// the signal is older than the most recently applied one (the newest-timestamp-wins guard
    /// against out-of-order/delayed signals — spec edge case).
    /// </summary>
    public bool ApplySignal(ActivitySignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);

        if (SessionState == SessionState.Ended)
        {
            return false;
        }

        if (ActivityChangedAt is { } current && signal.OccurredAt <= current)
        {
            return false;
        }

        if (signal.HookEvent == HookEvent.SessionEnd)
        {
            End(signal.OccurredAt);
            return true;
        }

        ActivityState = MapToActivityState(signal.HookEvent);
        ActivitySummary = signal.SummaryText;
        if (signal.TranscriptPath is not null)
        {
            TranscriptPath = signal.TranscriptPath;
        }

        ActivityChangedAt = signal.OccurredAt;
        return true;
    }

    private static ActivityState MapToActivityState(HookEvent hookEvent) => hookEvent switch
    {
        HookEvent.UserPromptSubmit or HookEvent.PreToolUse => ActivityState.Working,
        HookEvent.Stop => ActivityState.Idle,
        HookEvent.Notification => ActivityState.WaitingForInput,
        _ => throw new ArgumentOutOfRangeException(nameof(hookEvent), hookEvent, "Unhandled hook event."),
    };
}
