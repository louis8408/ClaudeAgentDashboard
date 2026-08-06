using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class DismissAgentCommandTests
{
    [Fact]
    public void Execute_Removes_An_Ended_Session_From_The_Active_List()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        session.End(DateTimeOffset.UtcNow);
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var command = new DismissAgentCommand(registry);

        command.Execute(session.Id);

        Assert.DoesNotContain(registry.GetAll(), s => s.Id == session.Id);
    }

    [Fact]
    public void Execute_Is_A_NoOp_For_A_Running_Session()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var command = new DismissAgentCommand(registry);

        command.Execute(session.Id);

        Assert.Contains(registry.GetAll(), s => s.Id == session.Id);
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
