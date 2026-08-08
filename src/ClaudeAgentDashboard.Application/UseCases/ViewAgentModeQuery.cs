using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UseCases;

/// <summary>An agent's current permission mode (manual/accept-edits/plan/auto), read from its own transcript.</summary>
public sealed class ViewAgentModeQuery(AgentSessionRegistry registry, IPermissionModeReader modeReader)
{
    public PermissionMode Execute(Guid agentSessionId)
    {
        var session = registry.FindById(agentSessionId);
        return session?.TranscriptPath is null ? PermissionMode.Unknown : modeReader.ReadLatestPermissionMode(session.TranscriptPath);
    }
}
