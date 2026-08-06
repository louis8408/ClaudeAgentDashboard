namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>
/// Installs and verifies the Claude Code hook commands <see cref="IAgentActivityFeed"/>
/// depends on (FR-013's one-time setup step).
/// </summary>
public interface IHookRegistrar
{
    /// <summary>Whether the required hook commands are already present in the user's Claude Code configuration.</summary>
    bool AreHooksRegistered();

    /// <summary>
    /// Writes/updates the hook commands to point at the dashboard's local listener address
    /// (research.md R9). MUST be idempotent, and MUST NOT remove or alter hook entries the
    /// user configured for purposes other than this dashboard.
    /// </summary>
    void RegisterHooks(Uri listenerBaseAddress);
}
