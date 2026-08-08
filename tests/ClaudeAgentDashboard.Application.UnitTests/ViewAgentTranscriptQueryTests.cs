using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class ViewAgentTranscriptQueryTests
{
    [Fact]
    public void Execute_Returns_Recent_Entries_For_A_Session_With_A_Known_TranscriptPath()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        session.ApplySignal(new ActivitySignal(
            "agent", HookEvent.PreToolUse, DateTimeOffset.UtcNow, transcriptPath: @"C:\transcripts\a.jsonl"));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var entries = new[] { new TranscriptEntry("user", "entry one"), new TranscriptEntry("assistant", "entry two") };
        var reader = new FakeTranscriptReader(entries);
        var query = new ViewAgentTranscriptQuery(registry, reader);

        var result = query.Execute(session.Id);

        Assert.Equal(entries, result);
        Assert.Equal(@"C:\transcripts\a.jsonl", reader.LastRequestedPath);
    }

    [Fact]
    public void Execute_Returns_Empty_When_Session_Has_No_TranscriptPath_Yet()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var reader = new FakeTranscriptReader([new TranscriptEntry("assistant", "should not be returned")]);
        var query = new ViewAgentTranscriptQuery(registry, reader);

        var result = query.Execute(session.Id);

        Assert.Empty(result);
        Assert.Null(reader.LastRequestedPath);
    }

    [Fact]
    public void Execute_Returns_Empty_For_An_Unknown_Session()
    {
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([]));
        var query = new ViewAgentTranscriptQuery(registry, new FakeTranscriptReader([]));

        var result = query.Execute(Guid.NewGuid());

        Assert.Empty(result);
    }

    private sealed class FakeAgentWatcher(IReadOnlyCollection<AgentSession> sessions) : IAgentWatcher
    {
#pragma warning disable CS0067
        public event Action<AgentSession>? SessionStarted;
        public event Action<AgentSession>? SessionEnded;
#pragma warning restore CS0067

        public IReadOnlyCollection<AgentSession> GetCurrentSessions() => sessions;
    }

    private sealed class FakeTranscriptReader(IReadOnlyList<TranscriptEntry> entries) : ITranscriptReader
    {
        public string? LastRequestedPath { get; private set; }

        public IReadOnlyList<TranscriptEntry> ReadRecentEntries(string transcriptPath, int maxEntries)
        {
            LastRequestedPath = transcriptPath;
            return entries;
        }
    }
}
