using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class HandleNotificationActivatedCommandTests
{
    [Fact]
    public void Execute_Focuses_The_Correct_Sessions_Window()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(42));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var focuser = new FakeWindowFocuser(FocusResult.Focused);
        var command = new HandleNotificationActivatedCommand(registry, focuser);

        var result = command.Execute(session.Id);

        Assert.Equal(FocusResult.Focused, result);
        Assert.Same(session.WindowReference, focuser.LastRequest);
    }

    [Fact]
    public void Execute_Returns_WindowNoLongerAvailable_When_Session_Is_Unknown()
    {
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([]));
        var focuser = new FakeWindowFocuser(FocusResult.Focused);
        var command = new HandleNotificationActivatedCommand(registry, focuser);

        var result = command.Execute(Guid.NewGuid());

        Assert.Equal(FocusResult.WindowNoLongerAvailable, result);
        Assert.Null(focuser.LastRequest);
    }

    private sealed class FakeAgentWatcher(IReadOnlyCollection<AgentSession> sessions) : IAgentWatcher
    {
#pragma warning disable CS0067
        public event Action<AgentSession>? SessionStarted;
        public event Action<AgentSession>? SessionEnded;
#pragma warning restore CS0067

        public IReadOnlyCollection<AgentSession> GetCurrentSessions() => sessions;
    }

    private sealed class FakeWindowFocuser(FocusResult result) : IWindowFocuser
    {
        public TerminalWindowReference? LastRequest { get; private set; }

        public FocusResult Focus(TerminalWindowReference reference)
        {
            LastRequest = reference;
            return result;
        }
    }
}
