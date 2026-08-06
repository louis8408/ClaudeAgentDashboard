using System.Collections.Concurrent;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application;

/// <summary>
/// The application's single in-memory source of truth for active <see cref="AgentSession"/>s.
/// Seeded from <see cref="IAgentWatcher.GetCurrentSessions"/> and kept in sync via its
/// events; use cases depend on this rather than talking to IAgentWatcher directly, since
/// correlating hook signals (research.md R10) and dismissing ended sessions (FR-012) both
/// need a shared, mutable view across the whole active session set.
/// </summary>
public sealed class AgentSessionRegistry
{
    private readonly ConcurrentDictionary<Guid, AgentSession> _sessions = new();

    public AgentSessionRegistry(IAgentWatcher agentWatcher)
    {
        ArgumentNullException.ThrowIfNull(agentWatcher);

        foreach (var session in agentWatcher.GetCurrentSessions())
        {
            _sessions.TryAdd(session.Id, session);
        }

        agentWatcher.SessionStarted += session => _sessions.TryAdd(session.Id, session);
        agentWatcher.SessionEnded += session => _sessions.TryAdd(session.Id, session);
    }

    public IReadOnlyCollection<AgentSession> GetAll() => [.. _sessions.Values];

    public AgentSession? FindById(Guid id) => _sessions.TryGetValue(id, out var session) ? session : null;

    /// <summary>
    /// Matches a hook signal's correlation key (cwd) against a still-running session
    /// (research.md R10/R15). Prefers the session's actual resolved <see cref="AgentSession.WorkingDirectory"/>
    /// when known (FR-018) — authoritative once available, since it's the real cwd rather than a
    /// guess — and falls back to the weaker command-line-derived <see cref="AgentSession.Label"/>
    /// substring match only for sessions whose working directory couldn't be resolved. Returns the
    /// most recently started match, or null if none — callers must tolerate a miss rather than treat
    /// it as an error, since a signal can legitimately arrive before its session has been detected yet.
    /// </summary>
    public AgentSession? FindByCorrelationKey(string correlationKey)
    {
        if (string.IsNullOrWhiteSpace(correlationKey))
        {
            return null;
        }

        var running = _sessions.Values.Where(s => s.SessionState == SessionState.Running);

        var byWorkingDirectory = running
            .Where(s => s.WorkingDirectory is not null && Overlaps(s.WorkingDirectory, correlationKey))
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefault();

        if (byWorkingDirectory is not null)
        {
            return byWorkingDirectory;
        }

        return running
            .Where(s => s.WorkingDirectory is null && Overlaps(s.Label, correlationKey))
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefault();
    }

    private static bool Overlaps(string a, string b) =>
        a.Contains(b, StringComparison.OrdinalIgnoreCase) || b.Contains(a, StringComparison.OrdinalIgnoreCase);

    /// <summary>Removes an Ended session from the active list (FR-012); a no-op for a Running one.</summary>
    public void Dismiss(Guid id)
    {
        if (_sessions.TryGetValue(id, out var session) && session.SessionState == SessionState.Ended)
        {
            _sessions.TryRemove(id, out _);
        }
    }
}
