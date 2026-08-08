using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Presentation.Theming;

/// <summary>
/// The command-center visual theme's full palette (FR-013 from 002-command-center-dashboard),
/// now in two variants. Applying a theme writes every key directly into
/// <c>Application.Current.Resources</c> — every view's <c>{DynamicResource}</c> binding picks
/// up the change immediately, and it's the one place both <see cref="Views.DesktopWindow"/>
/// and <see cref="Views.SettingsWindow"/> (siblings, not parent/child, so they can't share
/// per-Window resources) draw from.
/// </summary>
public static class ThemeResources
{
    public static void Apply(AppTheme theme)
    {
        var palette = theme == AppTheme.Light ? Light : Dark;
        var app = Avalonia.Application.Current!;
        var resources = app.Resources;

        foreach (var (key, value) in palette)
        {
            resources[key] = value;
        }

        resources["CcFontFamily"] = new FontFamily("Consolas,Menlo,Monospace");

        // Avalonia's built-in FluentTheme (stock CheckBox/RadioButton/ScrollBar styling, used
        // by SettingsWindow) otherwise follows RequestedThemeVariant="Default" — the OS theme —
        // independently of the Cc* palette above. Setting it here keeps stock controls in step
        // with this app's own Light/Dark choice instead of silently mismatching it.
        app.RequestedThemeVariant = theme == AppTheme.Light ? ThemeVariant.Light : ThemeVariant.Dark;
    }

    private static IReadOnlyDictionary<string, object> Dark { get; } = new Dictionary<string, object>
    {
        ["CcAccentBrush"] = new SolidColorBrush(Color.Parse("#00E5FF")),
        ["CcAccentDimBrush"] = new SolidColorBrush(Color.Parse("#5C00E5FF")),
        ["CcTextBrush"] = new SolidColorBrush(Color.Parse("#E8FBFF")),
        ["CcMutedTextBrush"] = new SolidColorBrush(Color.Parse("#8FB4BD")),
        ["CcBorderBrush"] = new SolidColorBrush(Color.Parse("#3300E5FF")),
        ["CcPanelBrush"] = new SolidColorBrush(Color.Parse("#B30A0F16")),
        ["CcSubtleFillBrush"] = new SolidColorBrush(Color.Parse("#1AFFFFFF")),
        ["CcOverlayBorderBrush"] = new SolidColorBrush(Color.Parse("#33FFFFFF")),
        ["CcScrimBrush"] = new SolidColorBrush(Color.Parse("#99000000")),
        ["CcBackgroundBrush"] = LinearGradient(("#FF05070C", 0), ("#FF0A0F16", 0.55), ("#FF060A10", 1)),
        ["CcOverlayBackgroundBrush"] = LinearGradient(("#E6202024", 0), ("#F01A1A1E", 1)),
    };

    private static IReadOnlyDictionary<string, object> Light { get; } = new Dictionary<string, object>
    {
        ["CcAccentBrush"] = new SolidColorBrush(Color.Parse("#0086A8")),
        ["CcAccentDimBrush"] = new SolidColorBrush(Color.Parse("#4D0086A8")),
        ["CcTextBrush"] = new SolidColorBrush(Color.Parse("#12181F")),
        ["CcMutedTextBrush"] = new SolidColorBrush(Color.Parse("#5B6772")),
        ["CcBorderBrush"] = new SolidColorBrush(Color.Parse("#332B7A94")),
        ["CcPanelBrush"] = new SolidColorBrush(Color.Parse("#F2FFFFFF")),
        ["CcSubtleFillBrush"] = new SolidColorBrush(Color.Parse("#14000000")),
        ["CcOverlayBorderBrush"] = new SolidColorBrush(Color.Parse("#332B7A94")),
        ["CcScrimBrush"] = new SolidColorBrush(Color.Parse("#66000000")),
        ["CcBackgroundBrush"] = LinearGradient(("#FFF4F7F9", 0), ("#FFE9EFF3", 0.55), ("#FFF0F4F6", 1)),
        ["CcOverlayBackgroundBrush"] = LinearGradient(("#FAFFFFFF", 0), ("#FAFBFDFF", 1)),
    };

    private static LinearGradientBrush LinearGradient(params (string Color, double Offset)[] stops)
    {
        var brush = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative) };
        foreach (var (color, offset) in stops)
        {
            brush.GradientStops.Add(new GradientStop(Color.Parse(color), offset));
        }

        return brush;
    }
}
