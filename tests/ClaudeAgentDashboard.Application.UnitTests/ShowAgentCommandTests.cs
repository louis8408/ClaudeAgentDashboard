using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class ShowAgentCommandTests
{
    [Fact]
    public void Execute_Calls_Focus_With_The_Sessions_WindowReference()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(42));
        var focuser = new FakeWindowFocuser(FocusResult.Focused);
        var command = new ShowAgentCommand(focuser);

        var result = command.Execute(session);

        Assert.Equal(FocusResult.Focused, result);
        Assert.Same(session.WindowReference, focuser.LastRequest);
    }

    [Fact]
    public void Execute_Surfaces_WindowNoLongerAvailable_From_The_Focuser()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(42));
        var focuser = new FakeWindowFocuser(FocusResult.WindowNoLongerAvailable);
        var command = new ShowAgentCommand(focuser);

        var result = command.Execute(session);

        Assert.Equal(FocusResult.WindowNoLongerAvailable, result);
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
