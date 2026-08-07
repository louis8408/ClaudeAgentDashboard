namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>Persists the small set of user preferences identified in the spec.</summary>
public interface ISettingsStore
{
    /// <summary>Reads/writes MUST be safe to call from the UI thread without blocking perceptibly.</summary>
    bool LaunchAtLoginEnabled { get; set; }

    /// <summary>
    /// Whether the dashboard's summary panel is collapsed to its compact strip. Same
    /// perceptibly-non-blocking, persist-immediately contract as <see cref="LaunchAtLoginEnabled"/>.
    /// </summary>
    bool SummaryPanelCollapsed { get; set; }
}
