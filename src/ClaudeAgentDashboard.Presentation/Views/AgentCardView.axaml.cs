using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>
/// User Story 5: one agent's card on the desktop surface — icon/label/status at a glance,
/// draggable to any position (research.md R12), and click-to-open the detail overlay
/// (distinguished from a drag by a small movement threshold).
/// </summary>
public partial class AgentCardView : UserControl
{
    private const double DragThresholdPixels = 4;

    private Point _pointerDownPosition;
    private Point _cardStartPosition;
    private bool _isDragging;

    public AgentSession Session { get; private set; } = null!;

    /// <summary>Raised on a plain click (no significant pointer movement between press and release).</summary>
    public event EventHandler? Clicked;

    /// <summary>Raised once, on release, at the end of a drag — never on every intermediate move.</summary>
    public event EventHandler<CardPosition>? PositionChanged;

    public AgentCardView()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
    }

    public void Bind(AgentSession session)
    {
        Session = session;
        LabelText.Text = session.Label;
        StatusText.Text = ActivityPresentation.DescribeCardStatus(session);

        var color = ActivityPresentation.ColorFor(session.SessionState, session.ActivityState);
        StatusDot.Fill = new SolidColorBrush(color);
        StatusText.Foreground = new SolidColorBrush(color);
        StatusBadge.Background = new SolidColorBrush(color, 0.16);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var parent = this.GetVisualParent();
        _pointerDownPosition = e.GetPosition(parent);
        _cardStartPosition = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        _isDragging = false;
        e.Pointer.Capture(this);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!ReferenceEquals(e.Pointer.Captured, this))
        {
            return;
        }

        var parent = this.GetVisualParent();
        var current = e.GetPosition(parent);
        var delta = current - _pointerDownPosition;

        if (!_isDragging && (Math.Abs(delta.X) > DragThresholdPixels || Math.Abs(delta.Y) > DragThresholdPixels))
        {
            _isDragging = true;
        }

        if (_isDragging)
        {
            Canvas.SetLeft(this, _cardStartPosition.X + delta.X);
            Canvas.SetTop(this, _cardStartPosition.Y + delta.Y);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (ReferenceEquals(e.Pointer.Captured, this))
        {
            e.Pointer.Capture(null);
        }

        if (_isDragging)
        {
            PositionChanged?.Invoke(this, new CardPosition(Canvas.GetLeft(this), Canvas.GetTop(this)));
        }
        else
        {
            Clicked?.Invoke(this, EventArgs.Empty);
        }

        _isDragging = false;
    }

}
