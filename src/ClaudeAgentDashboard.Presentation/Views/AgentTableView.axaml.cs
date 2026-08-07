using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>
/// The agent table (002-command-center-dashboard FR-002) replacing 001's freely-positioned
/// <c>AgentCardView</c> desktop surface — one row per detected <see cref="AgentSession"/>.
/// Rows are updated in place rather than data-bound, matching the codebase's existing
/// imperative Bind() convention (AgentSession raises no property-changed notifications).
/// </summary>
public partial class AgentTableView : UserControl
{
    private readonly Dictionary<Guid, RowElements> _rows = [];

    /// <summary>Raised when a row is clicked (not a "Show" action — there isn't one on the row itself).</summary>
    public event EventHandler<AgentSession>? AgentClicked;

    public AgentTableView()
    {
        InitializeComponent();
    }

    /// <summary>Adds/updates a row per session and removes rows for sessions no longer present.</summary>
    public void Render(IReadOnlyCollection<AgentSession> sessions)
    {
        var seenIds = new HashSet<Guid>();

        foreach (var session in sessions)
        {
            seenIds.Add(session.Id);

            if (_rows.TryGetValue(session.Id, out var existingRow))
            {
                Bind(existingRow, session);
                continue;
            }

            var row = CreateRow(session);
            Bind(row, session);
            RowsPanel.Children.Add(row.Root);
            _rows[session.Id] = row;
        }

        foreach (var staleId in _rows.Keys.Where(id => !seenIds.Contains(id)).ToList())
        {
            RowsPanel.Children.Remove(_rows[staleId].Root);
            _rows.Remove(staleId);
        }

        EmptyStatePanel.IsVisible = _rows.Count == 0;
    }

    private RowElements CreateRow(AgentSession session)
    {
        var label = new TextBlock
        {
            FontSize = 13, Foreground = Brushes.White, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var statusText = new TextBlock { FontSize = 11, FontWeight = Avalonia.Media.FontWeight.Medium };
        var badge = new Border
        {
            CornerRadius = new Avalonia.CornerRadius(7), Padding = new Avalonia.Thickness(8, 2),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Child = statusText,
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,140") };
        Grid.SetColumn(label, 0);
        Grid.SetColumn(badge, 1);
        grid.Children.Add(label);
        grid.Children.Add(badge);

        var root = new Border
        {
            Padding = new Avalonia.Thickness(16, 10), Cursor = new Cursor(StandardCursorType.Hand),
            Background = Brushes.Transparent, Child = grid,
        };
        root.PointerReleased += (_, e) =>
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                AgentClicked?.Invoke(this, session);
            }
        };

        return new RowElements(root, label, badge, statusText);
    }

    private static void Bind(RowElements row, AgentSession session)
    {
        row.Label.Text = session.Label;
        row.Status.Text = ActivityPresentation.DescribeCardStatus(session);

        var color = ActivityPresentation.ColorFor(session.SessionState, session.ActivityState);
        row.Status.Foreground = new SolidColorBrush(color);
        row.Badge.Background = new SolidColorBrush(color, 0.16);
    }

    private sealed record RowElements(Border Root, TextBlock Label, Border Badge, TextBlock Status);
}
