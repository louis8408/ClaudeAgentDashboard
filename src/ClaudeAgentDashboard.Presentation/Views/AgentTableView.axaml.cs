using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ClaudeAgentDashboard.Application.UseCases;
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
    private ViewAgentModeQuery? _viewAgentModeQuery;
    private ViewAgentDisplayNameQuery? _viewAgentDisplayNameQuery;

    /// <summary>Raised when a row is clicked (not a "Show" action — there isn't one on the row itself).</summary>
    public event EventHandler<AgentSession>? AgentClicked;

    public AgentTableView()
    {
        InitializeComponent();
    }

    /// <summary>Adds/updates a row per session and removes rows for sessions no longer present.</summary>
    public void Render(
        IReadOnlyCollection<AgentSession> sessions,
        ViewAgentModeQuery? viewAgentModeQuery = null,
        ViewAgentDisplayNameQuery? viewAgentDisplayNameQuery = null)
    {
        _viewAgentModeQuery = viewAgentModeQuery;
        _viewAgentDisplayNameQuery = viewAgentDisplayNameQuery;
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
            FontSize = 13, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        var modeText = new TextBlock { FontSize = 11, FontWeight = Avalonia.Media.FontWeight.Medium };
        var modeBadge = new Border
        {
            CornerRadius = new Avalonia.CornerRadius(7), Padding = new Avalonia.Thickness(8, 2),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Child = modeText,
        };
        var statusText = new TextBlock { FontSize = 11, FontWeight = Avalonia.Media.FontWeight.Medium };
        var statusBadge = new Border
        {
            CornerRadius = new Avalonia.CornerRadius(7), Padding = new Avalonia.Thickness(8, 2),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Child = statusText,
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,110,140") };
        Grid.SetColumn(label, 0);
        Grid.SetColumn(modeBadge, 1);
        Grid.SetColumn(statusBadge, 2);
        grid.Children.Add(label);
        grid.Children.Add(modeBadge);
        grid.Children.Add(statusBadge);

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

        return new RowElements(root, label, modeBadge, modeText, statusBadge, statusText);
    }

    private void Bind(RowElements row, AgentSession session)
    {
        row.Label.Text = _viewAgentDisplayNameQuery?.Execute(session.Id) is { } displayName && !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : session.Label;
        row.Label.Foreground = Avalonia.Application.Current!.FindResource("CcTextBrush") as IBrush ?? Brushes.White;

        var mode = _viewAgentModeQuery?.Execute(session.Id) ?? PermissionMode.Unknown;
        row.ModeText.Text = ModePresentation.Describe(mode);
        var modeColor = ModePresentation.ColorFor(mode);
        row.ModeText.Foreground = new SolidColorBrush(modeColor);
        row.ModeBadge.Background = new SolidColorBrush(modeColor, 0.16);

        row.Status.Text = ActivityPresentation.DescribeCardStatus(session);
        var statusColor = ActivityPresentation.ColorFor(session.SessionState, session.ActivityState);
        row.Status.Foreground = new SolidColorBrush(statusColor);
        row.StatusBadge.Background = new SolidColorBrush(statusColor, 0.16);
    }

    private sealed record RowElements(
        Border Root, TextBlock Label, Border ModeBadge, TextBlock ModeText, Border StatusBadge, TextBlock Status);
}
