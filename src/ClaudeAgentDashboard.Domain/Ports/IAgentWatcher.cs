namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>
/// Detects Claude Code agent sessions on the local machine and reports lifecycle changes
/// (SessionState only — fine-grained activity is <see cref="IAgentActivityFeed"/>'s job).
/// Implementations MUST NOT require the monitored process to be started by or registered
/// with this application (passive observation only).
/// </summary>
public interface IAgentWatcher
{
    /// <summary>
    /// Every currently known session (running or ended-but-not-dismissed), including
    /// sessions that were already running before the app itself started (FR-002, FR-010).
    /// </summary>
    IReadOnlyCollection<AgentSession> GetCurrentSessions();

    /// <summary>Raised when a new agent session is first detected (FR-004).</summary>
    event Action<AgentSession> SessionStarted;

    /// <summary>
    /// Raised exactly once when a previously-running session's process/window is confirmed
    /// gone (FR-006), for both normal completion and abnormal termination, independently of
    /// whether hooks are configured for that session.
    /// </summary>
    event Action<AgentSession> SessionEnded;
}
