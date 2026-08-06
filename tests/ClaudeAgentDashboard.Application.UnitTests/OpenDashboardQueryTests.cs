using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class OpenDashboardQueryTests
{
    [Fact]
    public void Execute_Returns_All_Sessions_From_The_Watcher()
    {
        var running = new AgentSession(Guid.NewGuid(), "already-running", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        var started = new AgentSession(Guid.NewGuid(), "newly-started", DateTimeOffset.UtcNow, new TerminalWindowReference(2));
        var watcher = new FakeAgentWatcher([running, started]);
        var query = new OpenDashboardQuery(watcher);

        var result = query.Execute();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Id == running.Id);
        Assert.Contains(result, s => s.Id == started.Id);
    }

    [Fact]
    public void Execute_Returns_Empty_When_No_Agents_Are_Running()
    {
        var watcher = new FakeAgentWatcher([]);
        var query = new OpenDashboardQuery(watcher);

        var result = query.Execute();

        Assert.Empty(result);
    }

    private sealed class FakeAgentWatcher(IReadOnlyCollection<AgentSession> sessions) : IAgentWatcher
    {
        // Not raised by this fake — these tests only exercise GetCurrentSessions().
#pragma warning disable CS0067
        public event Action<AgentSession>? SessionStarted;
        public event Action<AgentSession>? SessionEnded;
#pragma warning restore CS0067

        public IReadOnlyCollection<AgentSession> GetCurrentSessions() => sessions;
    }
}
