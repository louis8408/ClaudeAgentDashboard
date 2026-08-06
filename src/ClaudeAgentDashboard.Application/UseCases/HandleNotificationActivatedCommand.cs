using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UseCases;

/// <summary>
/// Backs User Story 3's click-to-focus behavior: resolves the session a notification
/// referred to and focuses its window, equivalently to <see cref="ShowAgentCommand"/>,
/// without requiring the dashboard window to be open first (FR-008).
/// </summary>
public sealed class HandleNotificationActivatedCommand(AgentSessionRegistry registry, IWindowFocuser windowFocuser)
{
    public FocusResult Execute(Guid agentSessionId)
    {
        var session = registry.FindById(agentSessionId);
        return session is null ? FocusResult.WindowNoLongerAvailable : windowFocuser.Focus(session.WindowReference);
    }
}
