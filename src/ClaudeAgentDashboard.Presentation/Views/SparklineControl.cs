using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>
/// A minimal custom-drawn trend line (002-command-center-dashboard research.md R5) — no
/// charting package: the summary panel needs exactly two simple series (tokens used,
/// running-agent count) with no axes, legends, or interactivity, so a small first-party
/// <see cref="Control"/> is smaller and has no third-party surface to track.
/// </summary>
public sealed class SparklineControl : Control
{
    public static readonly StyledProperty<IReadOnlyList<double>?> ValuesProperty =
        AvaloniaProperty.Register<SparklineControl, IReadOnlyList<double>?>(nameof(Values));

    public static readonly StyledProperty<IBrush> StrokeProperty =
        AvaloniaProperty.Register<SparklineControl, IBrush>(nameof(Stroke), Brushes.White);

    public IReadOnlyList<double>? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public IBrush Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    static SparklineControl()
    {
        AffectsRender<SparklineControl>(ValuesProperty, StrokeProperty);
    }

    public override void Render(DrawingContext context)
    {
        var values = Values;
        if (values is null || values.Count < 2 || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var min = values.Min();
        var max = values.Max();
        var range = max - min;

        var points = new Point[values.Count];
        for (var i = 0; i < values.Count; i++)
        {
            var x = values.Count == 1 ? 0 : Bounds.Width * i / (values.Count - 1);
            // A flat series (range == 0) still draws a centered horizontal line rather than
            // collapsing to a single point or dividing by zero.
            var normalized = range <= 0 ? 0.5 : (values[i] - min) / range;
            var y = Bounds.Height - (Bounds.Height * normalized);
            points[i] = new Point(x, y);
        }

        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(points[0], isFilled: false);
            for (var i = 1; i < points.Length; i++)
            {
                geometryContext.LineTo(points[i]);
            }

            geometryContext.EndFigure(isClosed: false);
        }

        context.DrawGeometry(null, new Pen(Stroke, 1.75), geometry);
    }
}
