using Avalonia.Controls;
using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>User Story 1: lists every currently detected agent with its status, or an empty state.</summary>
public partial class AgentListWindow : Window
{
    public AgentListWindow()
        : this([])
    {
    }

    public AgentListWindow(IReadOnlyCollection<AgentSession> sessions)
    {
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
}

/// <summary>Presentation-only display projection of an AgentSession — never leaks back into Domain.</summary>
public sealed record AgentListItem(string Label, string StatusText)
{
    public static AgentListItem From(AgentSession session) => new(
        session.Label,
        session.SessionState == SessionState.Ended ? "Ended" : DescribeActivity(session.ActivityState));

    private static string DescribeActivity(ActivityState state) => state switch
    {
        ActivityState.Working => "Working",
        ActivityState.Idle => "Idle",
        ActivityState.WaitingForInput => "Waiting for input",
        _ => "Running",
    };
}
