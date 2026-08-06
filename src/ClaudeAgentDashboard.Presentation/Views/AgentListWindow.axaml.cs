using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>
/// User Story 1: lists every currently detected agent with its status, or an empty state.
/// User Story 2: each entry's "Show" button focuses that agent's terminal window, informing
/// the user instead of failing silently if it is no longer available (FR-011).
/// User Story 3: status reflects live changes while the window is open, and ended entries
/// can be dismissed (FR-012).
/// </summary>
public partial class AgentListWindow : Window
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);

    private readonly OpenDashboardQuery? _openDashboardQuery;
    private readonly ShowAgentCommand _showAgentCommand;
    private readonly DismissAgentCommand? _dismissAgentCommand;
    private readonly ViewAgentActivityQuery? _viewAgentActivityQuery;
    private readonly DispatcherTimer? _refreshTimer;

    public AgentListWindow()
        : this(null, new ShowAgentCommand(new UnavailableWindowFocuser()), null, null)
    {
    }

    public AgentListWindow(
        OpenDashboardQuery? openDashboardQuery,
        ShowAgentCommand showAgentCommand,
        DismissAgentCommand? dismissAgentCommand,
        ViewAgentActivityQuery? viewAgentActivityQuery)
    {
        _openDashboardQuery = openDashboardQuery;
        _showAgentCommand = showAgentCommand;
        _dismissAgentCommand = dismissAgentCommand;
        _viewAgentActivityQuery = viewAgentActivityQuery;

        InitializeComponent();
        Render(openDashboardQuery?.Execute() ?? []);

        if (openDashboardQuery is not null)
        {
            _refreshTimer = new DispatcherTimer { Interval = RefreshInterval };
            _refreshTimer.Tick += (_, _) => Render(openDashboardQuery.Execute());
            _refreshTimer.Start();
            Closed += (_, _) => _refreshTimer.Stop();
        }
    }

    private void Render(IReadOnlyCollection<AgentSession> sessions)
    {
        var items = sessions.Select(AgentListItem.From).ToList();

        AgentItems.ItemsSource = items;
        AgentItems.IsVisible = items.Count > 0;
        EmptyStateText.IsVisible = items.Count == 0;
    }

    private void OnShowClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AgentSession session })
        {
            return;
        }

        var result = _showAgentCommand.Execute(session);
        ShowUnavailableMessageIfNeeded(session, result);
    }

    private void OnAgentEntryClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AgentSession session })
        {
            return;
        }

        var detailView = new AgentActivityDetailView(session.Label, session.Id, _viewAgentActivityQuery);
        detailView.Show();
        detailView.Activate();
    }

    private void OnDismissClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AgentSession session } || _dismissAgentCommand is null || _openDashboardQuery is null)
        {
            return;
        }

        _dismissAgentCommand.Execute(session.Id);
        Render(_openDashboardQuery.Execute());
    }

    private void ShowUnavailableMessageIfNeeded(AgentSession session, FocusResult result)
    {
        MessageBanner.IsVisible = result == FocusResult.WindowNoLongerAvailable;
        MessageBanner.Text = result == FocusResult.WindowNoLongerAvailable
            ? $"The window for '{session.Label}' is no longer available."
            : string.Empty;
    }

    /// <summary>Design-time/parameterless-constructor fallback only — never wired via DI.</summary>
    private sealed class UnavailableWindowFocuser : IWindowFocuser
    {
        public FocusResult Focus(TerminalWindowReference reference) => FocusResult.WindowNoLongerAvailable;
    }
}

/// <summary>Presentation-only display projection of an AgentSession — never leaks back into Domain.</summary>
public sealed record AgentListItem(string Label, string StatusText, bool IsEnded, AgentSession Session)
{
    public static AgentListItem From(AgentSession session) => new(
        session.Label,
        session.SessionState == SessionState.Ended ? "Ended" : DescribeActivity(session.ActivityState),
        session.SessionState == SessionState.Ended,
        session);

    private static string DescribeActivity(ActivityState state) => state switch
    {
        ActivityState.Working => "Working",
        ActivityState.Idle => "Idle",
        ActivityState.WaitingForInput => "Waiting for input",
        _ => "Running",
    };
}
