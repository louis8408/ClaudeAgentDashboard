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
    /// Best-effort match of a hook signal's correlation key (cwd) against a still-running
    /// session's label (research.md R10). Returns the most recently started match, or null
    /// if none — callers must tolerate a miss rather than treat it as an error, since a
    /// signal can legitimately arrive before its session has been detected yet.
    /// </summary>
    public AgentSession? FindByCorrelationKey(string correlationKey)
    {
        if (string.IsNullOrWhiteSpace(correlationKey))
        {
            return null;
        }

        return _sessions.Values
            .Where(s => s.SessionState == SessionState.Running)
            .Where(s => s.Label.Contains(correlationKey, StringComparison.OrdinalIgnoreCase)
                || correlationKey.Contains(s.Label, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefault();
    }

    /// <summary>Removes an Ended session from the active list (FR-012); a no-op for a Running one.</summary>
    public void Dismiss(Guid id)
    {
        if (_sessions.TryGetValue(id, out var session) && session.SessionState == SessionState.Ended)
        {
            _sessions.TryRemove(id, out _);
        }
    }
}
