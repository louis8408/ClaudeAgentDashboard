namespace ClaudeAgentDashboard.Domain;

/// <summary>
/// An OS-native notification raised when an <see cref="AgentSession"/> transitions into
/// Idle, WaitingForInput, or Ended (never Working) — spec FR-007, research.md R11.
/// </summary>
public sealed class AttentionNotification
{
    public Guid AgentSessionId { get; }
    public AttentionReason Reason { get; }
    public DateTimeOffset RaisedAt { get; }
    public bool WasDelivered { get; }

    public AttentionNotification(Guid agentSessionId, AttentionReason reason, DateTimeOffset raisedAt, bool wasDelivered)
    {
        AgentSessionId = agentSessionId;
        Reason = reason;
        RaisedAt = raisedAt;
        WasDelivered = wasDelivered;
    }
}
