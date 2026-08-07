using Avalonia.Controls;
using Avalonia.Threading;
using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>
/// The single main window — a command-center shell (002-command-center-dashboard) split into
/// a collapsible top summary region and a bottom agent-table region, replacing the freely
/// draggable card desktop surface from 001-agent-tray-dashboard (FR-001). Still hosts
/// <see cref="AgentDetailOverlay"/> over the same window rather than a second window — that
/// part of 001's design is unchanged, only what triggers it (a table row, not a card) changes.
/// </summary>
public partial class DesktopWindow : Window
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);

    private readonly ShowAgentCommand? _showAgentCommand;
    private readonly DismissAgentCommand? _dismissAgentCommand;
    private readonly ViewAgentActivityQuery? _viewAgentActivityQuery;
    private readonly ViewAgentTranscriptQuery? _viewAgentTranscriptQuery;
    private readonly IHookRegistrar? _hookRegistrar;
    private readonly Uri? _hookListenerBaseAddress;
    private readonly DispatcherTimer? _refreshTimer;
    private FleetSummaryPanel? _summaryPanel;
    private AgentDetailOverlay? _openOverlay;

    public DesktopWindow()
        : this(null, null, null, null, null, null, null, null, null)
    {
    }

    public DesktopWindow(
        OpenDashboardQuery? openDashboardQuery,
        ShowAgentCommand? showAgentCommand,
        DismissAgentCommand? dismissAgentCommand,
        ViewAgentActivityQuery? viewAgentActivityQuery,
        ViewAgentTranscriptQuery? viewAgentTranscriptQuery,
        ViewFleetSummaryQuery? viewFleetSummaryQuery,
        ISettingsStore? settingsStore,
        IHookRegistrar? hookRegistrar,
        Uri? hookListenerBaseAddress)
    {
        _showAgentCommand = showAgentCommand;
        _dismissAgentCommand = dismissAgentCommand;
        _viewAgentActivityQuery = viewAgentActivityQuery;
        _viewAgentTranscriptQuery = viewAgentTranscriptQuery;
        _hookRegistrar = hookRegistrar;
        _hookListenerBaseAddress = hookListenerBaseAddress;

        InitializeComponent();

        var table = new AgentTableView();
        table.AgentClicked += (_, session) => OpenOverlay(session);
        TableHost.Content = table;
        table.Render(openDashboardQuery?.Execute() ?? []);

        InitializeSummaryPanel(viewFleetSummaryQuery, settingsStore);

        if (openDashboardQuery is not null)
        {
            _refreshTimer = new DispatcherTimer { Interval = RefreshInterval };
            _refreshTimer.Tick += (_, _) => table.Render(openDashboardQuery.Execute());
            _refreshTimer.Start();
            Closed += (_, _) =>
            {
                _refreshTimer.Stop();
                _summaryPanel?.StopRefreshing();
                _openOverlay?.StopRefreshing();
            };
        }
    }

    private void InitializeSummaryPanel(ViewFleetSummaryQuery? viewFleetSummaryQuery, ISettingsStore? settingsStore)
    {
        var panel = new FleetSummaryPanel(viewFleetSummaryQuery);
        panel.SetCollapsedSilently(settingsStore?.SummaryPanelCollapsed ?? false);
        panel.CollapsedChanged += (_, collapsed) =>
        {
            if (settingsStore is not null)
            {
                settingsStore.SummaryPanelCollapsed = collapsed;
            }
        };

        _summaryPanel = panel;
        SummaryPanelHost.Content = panel;
    }

    private void OpenOverlay(AgentSession session)
    {
        // Switching to a different agent while the overlay is already open (FR-014) preserves
        // whichever display mode (standard/expanded) was active, rather than resetting to
        // standard every time a new row is clicked.
        var wasExpanded = _openOverlay?.IsExpanded ?? false;
        _openOverlay?.StopRefreshing();

        var overlay = new AgentDetailOverlay(
            session, _showAgentCommand, _dismissAgentCommand, _viewAgentActivityQuery, _viewAgentTranscriptQuery,
            _hookRegistrar, _hookListenerBaseAddress);
        overlay.CloseRequested += (_, _) => CloseOverlay();
        overlay.SetExpanded(wasExpanded);

        _openOverlay = overlay;
        OverlayHost.Content = overlay;
        OverlayScrim.IsVisible = true;
    }

    private void CloseOverlay()
    {
        _openOverlay?.StopRefreshing();
        _openOverlay = null;
        OverlayHost.Content = null;
        OverlayScrim.IsVisible = false;
    }
}
