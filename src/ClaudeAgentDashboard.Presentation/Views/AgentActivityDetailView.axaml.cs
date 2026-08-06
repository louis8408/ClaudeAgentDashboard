using Avalonia.Controls;
using Avalonia.Threading;
using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>
/// User Story 4: a small detail view showing what a specific agent is currently doing,
/// live-updating as its activity changes while the view is open.
/// </summary>
public partial class AgentActivityDetailView : Window
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);

    private readonly DispatcherTimer? _refreshTimer;

    public AgentActivityDetailView()
        : this("Agent", Guid.Empty, null)
    {
    }

    public AgentActivityDetailView(string agentLabel, Guid agentSessionId, ViewAgentActivityQuery? viewAgentActivityQuery)
    {
        InitializeComponent();
        AgentLabelText.Text = agentLabel;
        Render(viewAgentActivityQuery?.Execute(agentSessionId));

        if (viewAgentActivityQuery is not null)
        {
            _refreshTimer = new DispatcherTimer { Interval = RefreshInterval };
            _refreshTimer.Tick += (_, _) => Render(viewAgentActivityQuery.Execute(agentSessionId));
            _refreshTimer.Start();
            Closed += (_, _) => _refreshTimer.Stop();
        }
    }

    private void Render(AgentActivityView? activity)
    {
        if (activity is null)
        {
            ActivityStateText.Text = "Unavailable";
            ActivitySummaryText.Text = "This agent is no longer being tracked.";
            return;
        }

        ActivityStateText.Text = DescribeActivity(activity.ActivityState);
        ActivitySummaryText.Text = activity.ActivitySummary ?? "No further detail is available yet.";
    }

    private static string DescribeActivity(ActivityState state) => state switch
    {
        ActivityState.Working => "Working",
        ActivityState.Idle => "Idle",
        ActivityState.WaitingForInput => "Waiting for input",
        _ => "Unknown (activity detection requires hook setup)",
    };
}
