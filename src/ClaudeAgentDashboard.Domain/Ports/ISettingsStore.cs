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

    /// <summary>Whether an attention notification is raised when an agent goes idle. Defaults to true.</summary>
    bool NotifyOnIdle { get; set; }

    /// <summary>Whether an attention notification is raised when an agent needs input. Defaults to true.</summary>
    bool NotifyOnWaitingForInput { get; set; }

    /// <summary>Whether an attention notification is raised when an agent's session ends. Defaults to true.</summary>
    bool NotifyOnEnded { get; set; }

    /// <summary>The dashboard's visual theme. Defaults to <see cref="AppTheme.Dark"/> — the original command-center design.</summary>
    AppTheme Theme { get; set; }

    /// <summary>
    /// Whether closing the dashboard window hides it (leaving the app running in the tray)
    /// rather than disposing it. Defaults to true. Independent of the app's own lifetime — the
    /// process always survives window close via the tray icon regardless of this setting; this
    /// only controls whether the window instance itself is preserved or recreated next time.
    /// </summary>
    bool MinimizeToTrayOnClose { get; set; }
}
