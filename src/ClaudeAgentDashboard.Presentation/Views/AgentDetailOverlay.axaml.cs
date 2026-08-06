using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>
/// User Story 5 (replaces the User Story 4 <c>AgentActivityDetailView</c> and folds in User
/// Story 2's "Show" and User Story 3's "Dismiss"): an in-window overlay — not a separate
/// Window — showing what an agent is currently doing, live-updating while open, with its
/// available actions. Closing it is the caller's responsibility (see <see cref="CloseRequested"/>);
/// this control never closes itself since it has no window of its own to close.
/// </summary>
public partial class AgentDetailOverlay : UserControl
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);

    private readonly AgentSession _session;
    private readonly ShowAgentCommand? _showAgentCommand;
    private readonly DismissAgentCommand? _dismissAgentCommand;
    private readonly IHookRegistrar? _hookRegistrar;
    private readonly Uri? _hookListenerBaseAddress;
    private readonly DispatcherTimer? _refreshTimer;

    /// <summary>Raised when the user clicks the close button — the caller hides/removes this overlay.</summary>
    public event EventHandler? CloseRequested;

    public AgentDetailOverlay()
        : this(DesignTimeSession(), null, null, null, null, null, null)
    {
    }

    public AgentDetailOverlay(
        AgentSession session,
        ShowAgentCommand? showAgentCommand,
        DismissAgentCommand? dismissAgentCommand,
        ViewAgentActivityQuery? viewAgentActivityQuery,
        ViewAgentTranscriptQuery? viewAgentTranscriptQuery,
        IHookRegistrar? hookRegistrar,
        Uri? hookListenerBaseAddress)
    {
        _session = session;
        _showAgentCommand = showAgentCommand;
        _dismissAgentCommand = dismissAgentCommand;
        _hookRegistrar = hookRegistrar;
        _hookListenerBaseAddress = hookListenerBaseAddress;

        InitializeComponent();
        AgentLabelText.Text = session.Label;
        Render(viewAgentActivityQuery?.Execute(session.Id));
        RenderTranscript(viewAgentTranscriptQuery?.Execute(session.Id));

        if (viewAgentActivityQuery is not null || viewAgentTranscriptQuery is not null)
        {
            _refreshTimer = new DispatcherTimer { Interval = RefreshInterval };
            _refreshTimer.Tick += (_, _) =>
            {
                Render(viewAgentActivityQuery?.Execute(session.Id));
                RenderTranscript(viewAgentTranscriptQuery?.Execute(session.Id));
            };
            _refreshTimer.Start();
        }
    }

    /// <summary>Stops the live-refresh timer — call when this overlay is hidden/removed.</summary>
    public void StopRefreshing() => _refreshTimer?.Stop();

    private void Render(AgentActivityView? activity)
    {
        DismissButton.IsVisible = _session.SessionState == SessionState.Ended;

        if (activity is null)
        {
            SetActivityBadge("Unavailable", Colors.Gray);
            ActivitySummaryText.Text = "This agent is no longer being tracked.";
            HookSetupPanel.IsVisible = false;
            return;
        }

        SetActivityBadge(
            ActivityPresentation.DescribeActivityDetailed(activity.ActivityState),
            ActivityPresentation.ColorFor(_session.SessionState, activity.ActivityState));
        ActivitySummaryText.Text = activity.ActivitySummary ?? "No further detail is available yet.";
        RenderHookSetupGuidance(activity.ActivityState);
    }

    /// <summary>
    /// The "Unknown" status isn't an error — it means Claude Code hasn't been told to report
    /// its activity yet, which needs a one-time opt-in (FR-013). Surface that explanation and
    /// an actual way to act on it here, where the user actually encounters it, rather than only
    /// in the tray menu where they'd have no reason to think to look.
    /// </summary>
    private void RenderHookSetupGuidance(ActivityState activityState)
    {
        if (activityState != ActivityState.Unknown || _session.SessionState == SessionState.Ended || _hookRegistrar is null)
        {
            HookSetupPanel.IsVisible = false;
            return;
        }

        HookSetupPanel.IsVisible = true;

        if (_hookRegistrar.AreHooksRegistered())
        {
            // Claude Code snapshots hook configuration at session start and never re-reads it
            // mid-session (a deliberate security measure, not a bug on either side) — so a
            // session already running when "Set up activity detection…" was clicked will never
            // report activity no matter how long it runs; restarting it is the actual fix in
            // that case. If a session started *after* setup is still stuck Unknown, the more
            // likely cause is this app's own working-directory resolution failing for it
            // (WindowsWorkingDirectoryResolver/R15) — restarting that one won't help.
            HookSetupExplanationText.Text =
                "Activity detection is set up, but this session may have started before that — " +
                "Claude Code only reads hook configuration once, at startup, so restarting this " +
                "session is usually what's needed. If a session started after setup is still stuck " +
                "here, its working directory likely couldn't be resolved instead.";
            SetUpHooksButton.IsVisible = false;
        }
        else
        {
            HookSetupExplanationText.Text =
                "Claude Code hasn't been told to report its activity to this app yet. This is a " +
                "one-time, local setup step — it doesn't let the dashboard control your agents.";
            SetUpHooksButton.IsVisible = true;
        }
    }

    private void OnSetUpHooksClicked(object? sender, RoutedEventArgs e)
    {
        if (_hookRegistrar is null || _hookListenerBaseAddress is null)
        {
            return;
        }

        _hookRegistrar.RegisterHooks(_hookListenerBaseAddress);
        HookSetupExplanationText.Text =
            "Done — activity detection is set up. New agent sessions will report status " +
            "automatically; this one may need to be restarted to pick it up.";
        SetUpHooksButton.IsVisible = false;
    }

    /// <summary>
    /// Read-only, informational only (FR-019/spec Assumptions) — this view offers no way to
    /// send input back to the agent.
    /// </summary>
    private void RenderTranscript(IReadOnlyList<string>? entries)
    {
        var hasEntries = entries is { Count: > 0 };
        TranscriptSection.IsVisible = hasEntries;
        TranscriptItems.ItemsSource = hasEntries ? entries : null;
    }

    private void SetActivityBadge(string text, Color color)
    {
        ActivityStateText.Text = text;
        ActivityStateText.Foreground = new SolidColorBrush(color);
        ActivityBadge.Background = new SolidColorBrush(color, 0.18);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void OnShowClicked(object? sender, RoutedEventArgs e)
    {
        if (_showAgentCommand is null)
        {
            return;
        }

        var result = _showAgentCommand.Execute(_session);
        MessageBanner.IsVisible = result == FocusResult.WindowNoLongerAvailable;
        MessageBanner.Text = result == FocusResult.WindowNoLongerAvailable
            ? $"The window for '{_session.Label}' is no longer available."
            : string.Empty;
    }

    private void OnDismissClicked(object? sender, RoutedEventArgs e)
    {
        if (_dismissAgentCommand is null)
        {
            return;
        }

        _dismissAgentCommand.Execute(_session.Id);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Design-time/parameterless-constructor fallback only — never wired via DI.</summary>
    private static AgentSession DesignTimeSession() =>
        new(Guid.Empty, "Agent", DateTimeOffset.UtcNow, new TerminalWindowReference(0));
}
