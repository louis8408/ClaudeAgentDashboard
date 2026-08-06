using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class ViewAgentActivityQueryTests
{
    [Fact]
    public void Execute_Returns_The_Sessions_Current_Activity()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        session.ApplySignal(new ActivitySignal("agent", HookEvent.PreToolUse, DateTimeOffset.UtcNow, "Running tool: Read"));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var query = new ViewAgentActivityQuery(registry);

        var result = query.Execute(session.Id);

        Assert.NotNull(result);
        Assert.Equal(ActivityState.Working, result.ActivityState);
        Assert.Equal("Running tool: Read", result.ActivitySummary);
    }

    [Fact]
    public void Execute_Reflects_The_Most_Recently_Applied_Signal()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var query = new ViewAgentActivityQuery(registry);
        var now = DateTimeOffset.UtcNow;

        session.ApplySignal(new ActivitySignal("agent", HookEvent.PreToolUse, now, "Running tool: Read"));
        Assert.Equal(ActivityState.Working, query.Execute(session.Id)!.ActivityState);

        session.ApplySignal(new ActivitySignal("agent", HookEvent.Notification, now.AddSeconds(1), "Waiting for your input"));

        var result = query.Execute(session.Id);
        Assert.Equal(ActivityState.WaitingForInput, result!.ActivityState);
        Assert.Equal("Waiting for your input", result.ActivitySummary);
    }

    [Fact]
    public void Execute_Returns_Null_For_An_Unknown_Session()
    {
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([]));
        var query = new ViewAgentActivityQuery(registry);

        var result = query.Execute(Guid.NewGuid());

        Assert.Null(result);
    }

    private sealed class FakeAgentWatcher(IReadOnlyCollection<AgentSession> sessions) : IAgentWatcher
    {
#pragma warning disable CS0067
        public event Action<AgentSession>? SessionStarted;
        public event Action<AgentSession>? SessionEnded;
#pragma warning restore CS0067

        public IReadOnlyCollection<AgentSession> GetCurrentSessions() => sessions;
    }
}
