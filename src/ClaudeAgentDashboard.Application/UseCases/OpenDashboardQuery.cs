using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Application.UseCases;

/// <summary>Backs User Story 1: populates the agent list when the tray/menu-bar icon is clicked.</summary>
public sealed class OpenDashboardQuery(AgentSessionRegistry registry)
{
    public IReadOnlyCollection<AgentSession> Execute() => registry.GetAll();
}
