using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Application.UseCases;

/// <summary>Backs User Story 4: a human-readable summary of what an agent is currently doing.</summary>
public sealed class ViewAgentActivityQuery(AgentSessionRegistry registry)
{
    public AgentActivityView? Execute(Guid agentSessionId)
    {
        var session = registry.FindById(agentSessionId);
        return session is null ? null : new AgentActivityView(session.ActivityState, session.ActivitySummary);
    }
}

public sealed record AgentActivityView(ActivityState ActivityState, string? ActivitySummary);
