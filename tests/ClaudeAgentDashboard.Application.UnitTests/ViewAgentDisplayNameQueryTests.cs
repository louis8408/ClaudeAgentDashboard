using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class ViewAgentDisplayNameQueryTests
{
    [Fact]
    public void Execute_Prefers_The_AiGenerated_Title_When_Available()
    {
        var session = new AgentSession(
            Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1), @"C:\work\my-project");
        session.ApplySignal(new ActivitySignal("agent", HookEvent.PreToolUse, DateTimeOffset.UtcNow, transcriptPath: "t.jsonl"));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var reader = new FakeTitleReader(new Dictionary<string, string> { ["t.jsonl"] = "Debug PreToolUse hook errors" });
        var query = new ViewAgentDisplayNameQuery(registry, reader);

        var result = query.Execute(session.Id);

        Assert.Equal("Debug PreToolUse hook errors", result);
    }

    [Fact]
    public void Execute_Falls_Back_To_The_Working_Directorys_Final_Segment_When_No_Title_Yet()
    {
        var session = new AgentSession(
            Guid.NewGuid(), "raw command line label", DateTimeOffset.UtcNow, new TerminalWindowReference(1),
            @"C:\Users\louis\source\repos\ClaudeAgentDashboard");
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var query = new ViewAgentDisplayNameQuery(registry, new FakeTitleReader(new Dictionary<string, string>()));

        var result = query.Execute(session.Id);

        Assert.Equal("ClaudeAgentDashboard", result);
    }

    [Fact]
    public void Execute_Falls_Back_To_The_Sessions_Label_When_No_Title_And_No_WorkingDirectory()
    {
        var session = new AgentSession(Guid.NewGuid(), "raw command line label", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var query = new ViewAgentDisplayNameQuery(registry, new FakeTitleReader(new Dictionary<string, string>()));

        var result = query.Execute(session.Id);

        Assert.Equal("raw command line label", result);
    }

    [Fact]
    public void Execute_Falls_Back_To_The_Sessions_Label_For_An_Unknown_Session()
    {
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([]));
        var query = new ViewAgentDisplayNameQuery(registry, new FakeTitleReader(new Dictionary<string, string>()));

        var result = query.Execute(Guid.NewGuid());

        Assert.Equal(string.Empty, result);
    }

    private sealed class FakeAgentWatcher(IReadOnlyCollection<AgentSession> sessions) : IAgentWatcher
    {
#pragma warning disable CS0067
        public event Action<AgentSession>? SessionStarted;
        public event Action<AgentSession>? SessionEnded;
#pragma warning restore CS0067

        public IReadOnlyCollection<AgentSession> GetCurrentSessions() => sessions;
    }

    private sealed class FakeTitleReader(IReadOnlyDictionary<string, string> byPath) : IAgentTitleReader
    {
        public string? ReadLatestTitle(string transcriptPath) => byPath.TryGetValue(transcriptPath, out var title) ? title : null;
    }
}
