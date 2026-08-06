namespace ClaudeAgentDashboard.Application.UseCases;

/// <summary>Backs FR-012: removes a dismissed, ended agent from the active list.</summary>
public sealed class DismissAgentCommand(AgentSessionRegistry registry)
{
    public void Execute(Guid agentSessionId) => registry.Dismiss(agentSessionId);
}
