using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class ViewFleetSummaryQueryTests
{
    [Fact]
    public void Execute_Reports_The_Running_Agent_Count_From_The_Registry()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var query = new ViewFleetSummaryQuery(registry, new FakeUsageMetricsReader(new Dictionary<string, UsageSnapshot>()), new FleetMetricsHistory());

        var result = query.Execute();

        Assert.Equal(1, result.Current.RunningAgentCount);
    }

    [Fact]
    public void Execute_Includes_Usage_For_A_Session_With_A_Readable_Transcript()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        session.ApplySignal(new ActivitySignal("agent", HookEvent.PreToolUse, DateTimeOffset.UtcNow, transcriptPath: "t.jsonl"));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var usage = new UsageSnapshot(tokensUsed: 42, contextWindowTokensInUse: 10, readAt: DateTimeOffset.UtcNow);
        var reader = new FakeUsageMetricsReader(new Dictionary<string, UsageSnapshot> { ["t.jsonl"] = usage });
        var query = new ViewFleetSummaryQuery(registry, reader, new FleetMetricsHistory());

        var result = query.Execute();

        Assert.Equal(42, result.Current.TotalTokensUsed);
        Assert.False(result.Current.IsPartial);
    }

    [Fact]
    public void Execute_Marks_Partial_For_A_Running_Session_With_No_Transcript_Path_Yet()
    {
        var session = new AgentSession(Guid.NewGuid(), "agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var query = new ViewFleetSummaryQuery(registry, new FakeUsageMetricsReader(new Dictionary<string, UsageSnapshot>()), new FleetMetricsHistory());

        var result = query.Execute();

        Assert.True(result.Current.IsPartial);
    }

    [Fact]
    public void Execute_Appends_Each_Call_To_History_In_Order()
    {
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([]));
        var query = new ViewFleetSummaryQuery(registry, new FakeUsageMetricsReader(new Dictionary<string, UsageSnapshot>()), new FleetMetricsHistory());

        query.Execute();
        var second = query.Execute();

        Assert.Equal(2, second.History.Count);
    }

    private sealed class FakeAgentWatcher(IReadOnlyCollection<AgentSession> sessions) : IAgentWatcher
    {
#pragma warning disable CS0067
        public event Action<AgentSession>? SessionStarted;
        public event Action<AgentSession>? SessionEnded;
#pragma warning restore CS0067

        public IReadOnlyCollection<AgentSession> GetCurrentSessions() => sessions;
    }

    private sealed class FakeUsageMetricsReader(IReadOnlyDictionary<string, UsageSnapshot> byPath) : IUsageMetricsReader
    {
        public UsageSnapshot? TryReadLatestUsage(string transcriptPath) =>
            byPath.TryGetValue(transcriptPath, out var snapshot) ? snapshot : null;
    }
}
