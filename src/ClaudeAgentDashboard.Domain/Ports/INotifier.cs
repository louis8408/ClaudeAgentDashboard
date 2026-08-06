namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>
/// Raises an OS-native notification when an agent needs attention, and reports back when
/// the user activates (clicks) it. Callers (Application layer) are responsible for only
/// invoking <see cref="NotifyAttention"/> on a genuine transition into Idle/WaitingForInput/
/// Ended and never for Working, and for not re-raising for an already-unacknowledged
/// attention state (research.md R11, FR-007a) — this port performs no de-duplication itself.
/// </summary>
public interface INotifier
{
    /// <summary>
    /// Raises an <see cref="AttentionNotification"/> for the given session and reason.
    /// Returns whether delivery succeeded; MUST NOT throw when the OS denies notification
    /// permission (spec edge case) — returns false instead.
    /// </summary>
    Task<bool> NotifyAttention(AgentSession session, AttentionReason reason);

    /// <summary>
    /// Raised when the user clicks a previously-raised notification, carrying the id of the
    /// AgentSession it referred to (FR-008). MUST fire even if the main dashboard window is
    /// currently closed.
    /// </summary>
    event Action<Guid> NotificationActivated;
}
