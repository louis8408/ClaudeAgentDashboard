using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UseCases;

/// <summary>Backs User Story 1: populates the agent list when the tray/menu-bar icon is clicked.</summary>
public sealed class OpenDashboardQuery(IAgentWatcher agentWatcher)
{
    public IReadOnlyCollection<AgentSession> Execute() => agentWatcher.GetCurrentSessions();
}
