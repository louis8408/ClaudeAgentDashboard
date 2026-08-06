using Avalonia.Controls;
using Avalonia.Interactivity;
using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>
/// User Story 1: lists every currently detected agent with its status, or an empty state.
/// User Story 2: each entry's "Show" button focuses that agent's terminal window, informing
/// the user instead of failing silently if it is no longer available (FR-011).
/// </summary>
public partial class AgentListWindow : Window
{
    private readonly ShowAgentCommand _showAgentCommand;

    public AgentListWindow()
        : this([], new ShowAgentCommand(new UnavailableWindowFocuser()))
    {
    }

    public AgentListWindow(IReadOnlyCollection<AgentSession> sessions, ShowAgentCommand showAgentCommand)
    {
        _showAgentCommand = showAgentCommand;
        InitializeComponent();
        Render(sessions);
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
public sealed record AgentListItem(string Label, string StatusText, AgentSession Session)
{
    public static AgentListItem From(AgentSession session) => new(
        session.Label,
        session.SessionState == SessionState.Ended ? "Ended" : DescribeActivity(session.ActivityState),
        session);

    private static string DescribeActivity(ActivityState state) => state switch
    {
        ActivityState.Working => "Working",
        ActivityState.Idle => "Idle",
        ActivityState.WaitingForInput => "Waiting for input",
        _ => "Running",
    };
}
