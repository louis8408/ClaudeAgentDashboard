namespace ClaudeAgentDashboard.Application;

/// <summary>
/// A bounded, in-memory-only ring buffer of <see cref="FleetSummarySnapshot"/>s feeding the
/// summary panel's trend graphs (002-command-center-dashboard research.md R3). Never persisted
/// to disk — it starts empty on every application launch, per spec Assumptions (trend graphs
/// cover in-session history only, no retention requirement).
/// </summary>
public sealed class FleetMetricsHistory
{
    private const int Capacity = 120;

    private readonly object _lock = new();
    private readonly Queue<FleetSummarySnapshot> _samples = new(Capacity);

    /// <summary>Appends a sample, evicting the oldest one once <see cref="Capacity"/> is reached.</summary>
    public void Record(FleetSummarySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_lock)
        {
            if (_samples.Count == Capacity)
            {
                _samples.Dequeue();
            }

            _samples.Enqueue(snapshot);
        }
    }

    /// <summary>The current buffer, oldest first.</summary>
    public IReadOnlyList<FleetSummarySnapshot> GetHistory()
    {
        lock (_lock)
        {
            return [.. _samples];
        }
    }
}
