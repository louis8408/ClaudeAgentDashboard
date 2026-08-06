namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>
/// Reports an agent's fine-grained in-session activity (Working/Idle/WaitingForInput),
/// sourced from Claude Code hook signals (research.md R8). Requires the one-time hook
/// setup described by <see cref="IHookRegistrar"/>; sessions with no hooks configured
/// simply never receive signals and stay Unknown (FR-013).
/// </summary>
public interface IAgentActivityFeed
{
    /// <summary>
    /// Raised whenever a hook payload is received and parsed, before correlation to a
    /// specific <see cref="AgentSession"/> (correlation, per research.md R10, is an
    /// Application-layer concern, not this port's responsibility).
    /// </summary>
    event Action<ActivitySignal> SignalReceived;
}
