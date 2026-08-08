using Avalonia.Media;
using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>Shared label/color mapping for an agent's permission mode, so the table and detail overlay present it consistently.</summary>
internal static class ModePresentation
{
    public static string Describe(PermissionMode mode) => mode switch
    {
        PermissionMode.Manual => "Manual",
        PermissionMode.AcceptEdits => "Accept Edits",
        PermissionMode.Plan => "Plan",
        PermissionMode.Auto => "Auto",
        _ => "Unknown",
    };

    public static Color ColorFor(PermissionMode mode) => mode switch
    {
        PermissionMode.Manual => Color.Parse("#8FB4BD"),
        PermissionMode.AcceptEdits => Color.Parse("#4C9AFF"),
        PermissionMode.Plan => Color.Parse("#B78CFF"),
        PermissionMode.Auto => Color.Parse("#FFAB4C"),
        _ => Colors.Gray,
    };
}
