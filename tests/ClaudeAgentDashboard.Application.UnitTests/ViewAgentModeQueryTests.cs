using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class ViewAgentModeQueryTests
{
    [Fact]
    public void Execute_Returns_The_Mode_Read_From_The_Sessions_Transcript()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        session.ApplySignal(new ActivitySignal("agent", HookEvent.PreToolUse, DateTimeOffset.UtcNow, transcriptPath: "t.jsonl"));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var reader = new FakeModeReader(new Dictionary<string, PermissionMode> { ["t.jsonl"] = PermissionMode.Plan });
        var query = new ViewAgentModeQuery(registry, reader);

        var result = query.Execute(session.Id);

        Assert.Equal(PermissionMode.Plan, result);
    }

    [Fact]
    public void Execute_Returns_Unknown_When_Session_Has_No_TranscriptPath_Yet()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var query = new ViewAgentModeQuery(registry, new FakeModeReader(new Dictionary<string, PermissionMode>()));

        var result = query.Execute(session.Id);

        Assert.Equal(PermissionMode.Unknown, result);
    }

    [Fact]
    public void Execute_Returns_Unknown_For_An_Unknown_Session()
    {
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([]));
        var query = new ViewAgentModeQuery(registry, new FakeModeReader(new Dictionary<string, PermissionMode>()));

        var result = query.Execute(Guid.NewGuid());

        Assert.Equal(PermissionMode.Unknown, result);
    }

    private sealed class FakeAgentWatcher(IReadOnlyCollection<AgentSession> sessions) : IAgentWatcher
    {
#pragma warning disable CS0067
        public event Action<AgentSession>? SessionStarted;
        public event Action<AgentSession>? SessionEnded;
#pragma warning restore CS0067

        public IReadOnlyCollection<AgentSession> GetCurrentSessions() => sessions;
    }

    private sealed class FakeModeReader(IReadOnlyDictionary<string, PermissionMode> byPath) : IPermissionModeReader
    {
        public PermissionMode ReadLatestPermissionMode(string transcriptPath) =>
            byPath.TryGetValue(transcriptPath, out var mode) ? mode : PermissionMode.Unknown;
    }
}
