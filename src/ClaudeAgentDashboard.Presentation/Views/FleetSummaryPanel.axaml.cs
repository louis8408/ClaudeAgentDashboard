using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClaudeAgentDashboard.Application.UseCases;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>
/// The collapsible top summary panel (002-command-center-dashboard, User Story 2): fleet-wide
/// running-agent count, total tokens used, available context window, and trend graphs for the
/// first two. Collapse/expand persistence (FR-008) is the caller's responsibility (see
/// <see cref="CollapsedChanged"/>) — this view is settings-agnostic, matching how
/// <see cref="AgentDetailOverlay"/> leaves closing itself to its caller.
/// </summary>
public partial class FleetSummaryPanel : UserControl
{
    // Timer-only cadence (research.md R3), simplified from "timer + registry-change event" —
    // a fixed 30s tick alone already avoids the flat-graph-gap problem R3 was written to avoid.
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

    private readonly DispatcherTimer? _refreshTimer;
    private bool _isCollapsed;

    /// <summary>Raised whenever collapse state changes, so the caller can persist it (FR-008).</summary>
    public event EventHandler<bool>? CollapsedChanged;

    public FleetSummaryPanel()
        : this(null)
    {
    }

    public FleetSummaryPanel(ViewFleetSummaryQuery? viewFleetSummaryQuery)
    {
        InitializeComponent();

        Render(viewFleetSummaryQuery?.Execute());

        if (viewFleetSummaryQuery is not null)
        {
            _refreshTimer = new DispatcherTimer { Interval = RefreshInterval };
            _refreshTimer.Tick += (_, _) => Render(viewFleetSummaryQuery.Execute());
            _refreshTimer.Start();
        }
    }

    /// <summary>Stops the live-refresh timer — call when this panel is no longer displayed.</summary>
    public void StopRefreshing() => _refreshTimer?.Stop();

    /// <summary>Sets collapsed state without raising <see cref="CollapsedChanged"/> — used at startup to apply a persisted value.</summary>
    public void SetCollapsedSilently(bool collapsed)
    {
        _isCollapsed = collapsed;
        ApplyCollapsedVisuals(collapsed);
    }

    private void OnToggleClicked(object? sender, RoutedEventArgs e)
    {
        _isCollapsed = !_isCollapsed;
        ApplyCollapsedVisuals(_isCollapsed);
        CollapsedChanged?.Invoke(this, _isCollapsed);
    }

    private void ApplyCollapsedVisuals(bool collapsed)
    {
        BodyPanel.IsVisible = !collapsed;
        CollapsedSummaryText.IsVisible = collapsed;
        ToggleButton.Content = collapsed ? "Expand ▾" : "Collapse ▴";
    }

    private void Render(FleetSummaryView? view)
    {
        if (view is null)
        {
            return;
        }

        var current = view.Current;
        RunningCountText.Text = current.RunningAgentCount.ToString();
        TokensUsedText.Text = FormatCount(current.TotalTokensUsed);
        ContextAvailableText.Text = FormatCount(current.TotalContextWindowAvailable);
        PartialNoticeText.IsVisible = current.IsPartial;
        CollapsedSummaryText.Text = $"{current.RunningAgentCount} running · {FormatCount(current.TotalTokensUsed)} tokens used";

        RunningCountSparkline.Values = [.. view.History.Select(s => (double)s.RunningAgentCount)];
        TokensUsedSparkline.Values = [.. view.History.Select(s => (double)s.TotalTokensUsed)];
    }

    private static string FormatCount(long value) => value >= 1_000
        ? $"{value / 1_000.0:0.#}K"
        : value.ToString();
}
