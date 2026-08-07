using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Application;

/// <summary>
/// A point-in-time aggregate over all currently detected Agent Sessions — the "Fleet Summary
/// Snapshot" entity (002-command-center-dashboard data-model.md). Not persisted; short-lived,
/// computed fresh by <see cref="FleetSummaryCalculator"/> and optionally retained by
/// <see cref="FleetMetricsHistory"/> for trend graphs.
/// </summary>
public sealed record FleetSummarySnapshot(
    int RunningAgentCount,
    long TotalTokensUsed,
    long TotalContextWindowAvailable,
    bool IsPartial,
    DateTimeOffset CapturedAt);

/// <summary>
/// Folds the current session set plus each one's latest usage into a <see cref="FleetSummarySnapshot"/>
/// (contracts/domain-ports.md). A pure, stateless computation over already-available data — no
/// port/Infrastructure dependency of its own; the caller supplies usage via <paramref name="usageLookup"/>.
/// </summary>
public static class FleetSummaryCalculator
{
    public static FleetSummarySnapshot Calculate(
        IReadOnlyCollection<AgentSession> sessions, Func<AgentSession, UsageSnapshot?> usageLookup)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(usageLookup);

        var runningAgentCount = 0;
        long totalTokensUsed = 0;
        long totalContextWindowAvailable = 0;
        var isPartial = false;

        foreach (var session in sessions)
        {
            var isRunning = session.SessionState == SessionState.Running;
            if (isRunning)
            {
                runningAgentCount++;
            }

            var usage = usageLookup(session);
            if (usage is null)
            {
                if (isRunning)
                {
                    isPartial = true;
                }

                continue;
            }

            totalTokensUsed += usage.TokensUsed;
            totalContextWindowAvailable += usage.ContextWindowTokensAvailable;
        }

        return new FleetSummarySnapshot(
            runningAgentCount, totalTokensUsed, totalContextWindowAvailable, isPartial, DateTimeOffset.UtcNow);
    }
}
