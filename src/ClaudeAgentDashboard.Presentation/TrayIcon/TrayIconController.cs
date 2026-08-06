using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace ClaudeAgentDashboard.Presentation.TrayIcon;

/// <summary>
/// Owns the persistent tray/menu-bar icon (FR-001). User Story 1 wires the click-to-open
/// behavior; User Story 3 adds the attention-needed badge on top of this baseline.
/// </summary>
public sealed class TrayIconController : IDisposable
{
    // avares:// is Avalonia's resource-asset scheme, not a real filesystem/network path.
#pragma warning disable S1075
    private const string IconResourceUri = "avares://ClaudeAgentDashboard.Presentation/Assets/tray-icon.ico";
#pragma warning restore S1075

    private readonly NativeMenuItem _quitItem;

    /// <summary>Raised when the user clicks the tray/menu-bar icon (User Story 1).</summary>
    public event EventHandler? DashboardRequested;

    public TrayIconController()
    {
        _quitItem = new NativeMenuItem("Quit");
        _quitItem.Click += OnQuitClicked;

        var menu = new NativeMenu { Items = { _quitItem } };

        var trayIcon = new Avalonia.Controls.TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri(IconResourceUri))),
            ToolTipText = "Claude Agent Dashboard",
            Menu = menu,
        };
        trayIcon.Clicked += OnTrayIconClicked;

        Avalonia.Controls.TrayIcon.SetIcons(Avalonia.Application.Current!, new TrayIcons { trayIcon });
    }

    private void OnTrayIconClicked(object? sender, EventArgs e) => DashboardRequested?.Invoke(this, EventArgs.Empty);

    private static void OnQuitClicked(object? sender, EventArgs e)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public void Dispose()
    {
        _quitItem.Click -= OnQuitClicked;
        Avalonia.Controls.TrayIcon.SetIcons(Avalonia.Application.Current!, null!);
    }
}
