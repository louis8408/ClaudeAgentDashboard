using Avalonia.Media;
using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>
/// Shared label/color mapping for an agent's status, so the card and its detail overlay
/// present the same activity consistently rather than drifting apart independently.
/// </summary>
internal static class ActivityPresentation
{
    /// <summary>Short form for the compact card — a full sentence wouldn't fit.</summary>
    public static string DescribeCardStatus(AgentSession session) => session.SessionState == SessionState.Ended
        ? "Ended"
        : session.ActivityState switch
        {
            ActivityState.Working => "Working",
            ActivityState.Idle => "Idle",
            ActivityState.WaitingForInput => "Waiting for input",
            _ => "Running",
        };

    /// <summary>Longer form for the detail overlay, where an unregistered-hooks explanation fits.</summary>
    public static string DescribeActivityDetailed(ActivityState state) => state switch
    {
        ActivityState.Working => "Working",
        ActivityState.Idle => "Idle",
        ActivityState.WaitingForInput => "Waiting for input",
        _ => "Unknown (activity detection requires hook setup)",
    };

    public static Color ColorFor(SessionState sessionState, ActivityState activityState)
    {
        if (sessionState == SessionState.Ended)
        {
            return Colors.Gray;
        }

        return activityState switch
        {
            ActivityState.Working => Color.Parse("#4C9AFF"),
            ActivityState.Idle => Color.Parse("#FFAB4C"),
            ActivityState.WaitingForInput => Color.Parse("#FF6B6B"),
            _ => Colors.Gray,
        };
    }
}
