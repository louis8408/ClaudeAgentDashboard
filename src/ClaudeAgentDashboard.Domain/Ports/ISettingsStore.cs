namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>Persists the small set of user preferences identified in the spec.</summary>
public interface ISettingsStore
{
    /// <summary>Reads/writes MUST be safe to call from the UI thread without blocking perceptibly.</summary>
    bool LaunchAtLoginEnabled { get; set; }

    /// <summary>Absolute path to the user-selected desktop background image, or null for the default.</summary>
    string? BackgroundImagePath { get; set; }

    /// <summary>
    /// The saved card position for the agent identity with this label, or null if this label
    /// has no saved position yet (the caller applies a default, non-overlapping placement).
    /// </summary>
    CardPosition? GetCardPosition(string agentLabel);

    /// <summary>Persists a card's position immediately — called on drag-release only, not on every move.</summary>
    void SetCardPosition(string agentLabel, CardPosition position);
}
