using ClaudeAgentDashboard.Application;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class FleetMetricsHistoryTests
{
    private static FleetSummarySnapshot Snapshot(int runningAgentCount) =>
        new(runningAgentCount, TotalTokensUsed: 0, TotalContextWindowAvailable: 0, IsPartial: false, DateTimeOffset.UtcNow);

    [Fact]
    public void GetHistory_Returns_Empty_When_Nothing_Recorded_Yet()
    {
        var history = new FleetMetricsHistory();

        Assert.Empty(history.GetHistory());
    }

    [Fact]
    public void GetHistory_Returns_Recorded_Snapshots_Oldest_First()
    {
        var history = new FleetMetricsHistory();

        history.Record(Snapshot(1));
        history.Record(Snapshot(2));
        history.Record(Snapshot(3));

        var recorded = history.GetHistory();
        Assert.Equal(3, recorded.Count);
        Assert.Equal([1, 2, 3], recorded.Select(s => s.RunningAgentCount));
    }

    [Fact]
    public void Record_Evicts_The_Oldest_Sample_Once_The_120Sample_Cap_Is_Reached()
    {
        var history = new FleetMetricsHistory();

        for (var i = 0; i < 121; i++)
        {
            history.Record(Snapshot(i));
        }

        var recorded = history.GetHistory();
        Assert.Equal(120, recorded.Count);
        Assert.Equal(1, recorded[0].RunningAgentCount); // sample 0 evicted, oldest surviving is 1
        Assert.Equal(120, recorded[^1].RunningAgentCount);
    }

    [Fact]
    public void Record_And_GetHistory_Are_Safe_To_Call_Concurrently()
    {
        var history = new FleetMetricsHistory();

        Parallel.For(0, 500, i =>
        {
            history.Record(Snapshot(i));
            _ = history.GetHistory().Count;
        });

        // No exception thrown is the assertion here; the cap must still hold under contention.
        Assert.True(history.GetHistory().Count <= 120);
    }
}
