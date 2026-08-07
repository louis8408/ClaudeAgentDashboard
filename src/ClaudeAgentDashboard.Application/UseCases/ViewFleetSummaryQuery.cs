using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UseCases;

/// <summary>
/// Backs User Story 2 (002-command-center-dashboard): the summary panel's fleet-wide figures
/// and trend-graph history. Every call recomputes the current <see cref="FleetSummarySnapshot"/>
/// and records it into <see cref="FleetMetricsHistory"/> — the caller (composition root, T024)
/// is responsible for only calling this at the intended cadence (a registry-change event or the
/// 30-second timer, research.md R3), not on every UI repaint.
/// </summary>
public sealed class ViewFleetSummaryQuery(
    AgentSessionRegistry registry, IUsageMetricsReader usageMetricsReader, FleetMetricsHistory history)
{
    public FleetSummaryView Execute()
    {
        var sessions = registry.GetAll();
        var snapshot = FleetSummaryCalculator.Calculate(sessions, LookupUsage);
        history.Record(snapshot);

        return new FleetSummaryView(snapshot, history.GetHistory());
    }

    private UsageSnapshot? LookupUsage(AgentSession session) =>
        session.TranscriptPath is null ? null : usageMetricsReader.TryReadLatestUsage(session.TranscriptPath);
}

public sealed record FleetSummaryView(FleetSummarySnapshot Current, IReadOnlyList<FleetSummarySnapshot> History);
