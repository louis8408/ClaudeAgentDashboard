using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UseCases;

/// <summary>Backs User Story 2: brings an agent's terminal window to the foreground.</summary>
public sealed class ShowAgentCommand(IWindowFocuser windowFocuser)
{
    public FocusResult Execute(AgentSession session) => windowFocuser.Focus(session.WindowReference);
}
