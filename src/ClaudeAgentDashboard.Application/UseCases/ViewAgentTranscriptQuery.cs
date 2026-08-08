using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UseCases;

/// <summary>Backs FR-019: a read-only, recent excerpt of an agent's own transcript.</summary>
public sealed class ViewAgentTranscriptQuery(AgentSessionRegistry registry, ITranscriptReader transcriptReader)
{
    private const int MaxEntries = 20;

    public IReadOnlyList<TranscriptEntry> Execute(Guid agentSessionId)
    {
        var session = registry.FindById(agentSessionId);
        if (session?.TranscriptPath is null)
        {
            return [];
        }

        return transcriptReader.ReadRecentEntries(session.TranscriptPath, MaxEntries);
    }
}
