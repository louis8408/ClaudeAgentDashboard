using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UseCases;

/// <summary>
/// A human-friendly name for an agent, replacing the raw command-line-derived
/// <see cref="Domain.AgentSession.Label"/> (e.g. a bare "claude" invocation's label is just its
/// own exe path) with the best available of, in order: Claude Code's own AI-generated session
/// title, the working directory's final path segment (the project/folder name), or — if
/// neither is available yet — the session's own label as a last resort.
/// </summary>
public sealed class ViewAgentDisplayNameQuery(AgentSessionRegistry registry, IAgentTitleReader titleReader)
{
    public string Execute(Guid agentSessionId)
    {
        var session = registry.FindById(agentSessionId);
        if (session is null)
        {
            return string.Empty;
        }

        var title = session.TranscriptPath is not null ? titleReader.ReadLatestTitle(session.TranscriptPath) : null;
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title;
        }

        var projectName = session.WorkingDirectory is not null
            ? Path.GetFileName(session.WorkingDirectory.TrimEnd('\\', '/'))
            : null;

        return !string.IsNullOrWhiteSpace(projectName) ? projectName : session.Label;
    }
}
