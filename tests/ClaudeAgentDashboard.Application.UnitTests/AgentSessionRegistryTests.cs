using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class AgentSessionRegistryTests
{
    [Fact]
    public void FindByCorrelationKey_Matches_On_WorkingDirectory_When_Set()
    {
        var session = new AgentSession(
            Guid.NewGuid(), "\"C:\\Users\\louis\\.local\\bin\\claude.exe\"", DateTimeOffset.UtcNow,
            new TerminalWindowReference(1), workingDirectory: @"C:\work\my-project");
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));

        var found = registry.FindByCorrelationKey(@"C:\work\my-project");

        Assert.Same(session, found);
    }

    [Fact]
    public void FindByCorrelationKey_Does_Not_Match_Label_When_WorkingDirectory_Is_Set_But_Different()
    {
        // The exact real-world bug this guards against: a bare `claude` invocation's command-line
        // label never contains its actual working directory, so once a real WorkingDirectory is
        // known it must be authoritative — falling back to the label here would silently "fix"
        // a wrong match instead of correctly reporting no match.
        var session = new AgentSession(
            Guid.NewGuid(), "\"C:\\Users\\louis\\.local\\bin\\claude.exe\"", DateTimeOffset.UtcNow,
            new TerminalWindowReference(1), workingDirectory: @"C:\work\my-project");
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));

        var found = registry.FindByCorrelationKey(@"C:\work\unrelated-project");

        Assert.Null(found);
    }

    [Fact]
    public void FindByCorrelationKey_Falls_Back_To_Label_When_WorkingDirectory_Is_Null()
    {
        var session = new AgentSession(
            Guid.NewGuid(), @"C:\work\my-project", DateTimeOffset.UtcNow, new TerminalWindowReference(1),
            workingDirectory: null);
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));

        var found = registry.FindByCorrelationKey(@"C:\work\my-project");

        Assert.Same(session, found);
    }

    [Fact]
    public void FindByCorrelationKey_Returns_Null_When_Nothing_Matches()
    {
        var session = new AgentSession(
            Guid.NewGuid(), @"C:\work\my-project", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));

        var found = registry.FindByCorrelationKey(@"C:\completely\different");

        Assert.Null(found);
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
