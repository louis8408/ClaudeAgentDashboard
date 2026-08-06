namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>Persists the small set of user preferences identified in the spec.</summary>
public interface ISettingsStore
{
    /// <summary>Reads/writes MUST be safe to call from the UI thread without blocking perceptibly.</summary>
    bool LaunchAtLoginEnabled { get; set; }
}
