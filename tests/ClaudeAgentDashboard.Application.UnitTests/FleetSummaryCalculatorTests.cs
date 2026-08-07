using ClaudeAgentDashboard.Application;
using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class FleetSummaryCalculatorTests
{
    private static AgentSession NewRunningSession(string label = "agent") =>
        new(Guid.NewGuid(), label, DateTimeOffset.UtcNow, new TerminalWindowReference(1));

    [Fact]
    public void Calculate_Counts_Only_Running_Sessions_Regardless_Of_Usage_Availability()
    {
        var running = NewRunningSession();
        var ended = NewRunningSession();
        ended.End(DateTimeOffset.UtcNow);

        var snapshot = FleetSummaryCalculator.Calculate([running, ended], _ => null);

        Assert.Equal(1, snapshot.RunningAgentCount);
    }

    [Fact]
    public void Calculate_Sums_Totals_Only_For_Sessions_With_A_UsageSnapshot()
    {
        var withUsage = NewRunningSession("has-usage");
        var withoutUsage = NewRunningSession("no-usage");
        var usage = new UsageSnapshot(tokensUsed: 500, contextWindowTokensInUse: 1_000, readAt: DateTimeOffset.UtcNow);

        var snapshot = FleetSummaryCalculator.Calculate(
            [withUsage, withoutUsage],
            session => session == withUsage ? usage : null);

        Assert.Equal(500, snapshot.TotalTokensUsed);
        Assert.Equal(usage.ContextWindowTokensAvailable, snapshot.TotalContextWindowAvailable);
    }

    [Fact]
    public void Calculate_Sets_IsPartial_When_A_Running_Session_Has_No_UsageSnapshot()
    {
        var running = NewRunningSession();

        var snapshot = FleetSummaryCalculator.Calculate([running], _ => null);

        Assert.True(snapshot.IsPartial);
    }

    [Fact]
    public void Calculate_Is_Not_Partial_When_Every_Running_Session_Has_A_UsageSnapshot()
    {
        var running = NewRunningSession();
        var usage = new UsageSnapshot(tokensUsed: 10, contextWindowTokensInUse: 20, readAt: DateTimeOffset.UtcNow);

        var snapshot = FleetSummaryCalculator.Calculate([running], _ => usage);

        Assert.False(snapshot.IsPartial);
    }

    [Fact]
    public void Calculate_Returns_Zeroed_Non_Partial_Snapshot_For_No_Sessions()
    {
        var snapshot = FleetSummaryCalculator.Calculate([], _ => null);

        Assert.Equal(0, snapshot.RunningAgentCount);
        Assert.Equal(0, snapshot.TotalTokensUsed);
        Assert.Equal(0, snapshot.TotalContextWindowAvailable);
        Assert.False(snapshot.IsPartial);
    }
}
