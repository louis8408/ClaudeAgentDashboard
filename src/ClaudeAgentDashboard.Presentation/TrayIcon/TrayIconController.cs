using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Presentation.TrayIcon;

/// <summary>
/// Owns the persistent tray/menu-bar icon (FR-001), click-to-open (User Story 1), the
/// attention-needed badge and hook setup action (User Story 3, FR-009/FR-013).
/// </summary>
public sealed class TrayIconController : IDisposable
{
    private const string DefaultTooltip = "Claude Agent Dashboard";
    private static readonly TimeSpan BadgeRefreshInterval = TimeSpan.FromSeconds(2);

    // avares:// is Avalonia's resource-asset scheme, not a real filesystem/network path.
#pragma warning disable S1075
    private const string IconResourceUri = "avares://ClaudeAgentDashboard.Presentation/Assets/tray-icon.ico";
#pragma warning restore S1075

    private readonly NativeMenuItem _quitItem;
    private readonly Avalonia.Controls.TrayIcon _trayIcon;
    private readonly DispatcherTimer? _badgeTimer;

    /// <summary>Raised when the user clicks the tray/menu-bar icon (User Story 1).</summary>
    public event EventHandler? DashboardRequested;

    /// <summary>Raised when the user picks "Settings…" from the tray menu.</summary>
    public event EventHandler? SettingsRequested;

    public TrayIconController(OpenDashboardQuery? openDashboardQuery = null, IHookRegistrar? hookRegistrar = null, Uri? hookListenerBaseAddress = null)
    {
        _quitItem = new NativeMenuItem("Quit");
        _quitItem.Click += OnQuitClicked;

        var settingsItem = new NativeMenuItem("Settings…");
        settingsItem.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);

        var menu = new NativeMenu { Items = { settingsItem, _quitItem } };

        if (hookRegistrar is not null && hookListenerBaseAddress is not null)
        {
            var setupItem = new NativeMenuItem("Set up activity detection…");
            setupItem.Click += (_, _) => hookRegistrar.RegisterHooks(hookListenerBaseAddress);
            menu.Items.Insert(0, setupItem);
        }

        _trayIcon = new Avalonia.Controls.TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri(IconResourceUri))),
            ToolTipText = DefaultTooltip,
            Menu = menu,
            IsVisible = true,
        };
        _trayIcon.Clicked += OnTrayIconClicked;

        Avalonia.Controls.TrayIcon.SetIcons(Avalonia.Application.Current!, new TrayIcons { _trayIcon });

        if (openDashboardQuery is not null)
        {
            _badgeTimer = new DispatcherTimer { Interval = BadgeRefreshInterval };
            _badgeTimer.Tick += (_, _) => RefreshBadge(openDashboardQuery);
            _badgeTimer.Start();
        }
    }

    private void RefreshBadge(OpenDashboardQuery openDashboardQuery)
    {
        var needingAttention = openDashboardQuery.Execute()
            .Count(s => s.ActivityState is ActivityState.Idle or ActivityState.WaitingForInput);

        _trayIcon.ToolTipText = needingAttention switch
        {
            0 => DefaultTooltip,
            1 => $"{DefaultTooltip} — 1 agent needs your attention",
            _ => $"{DefaultTooltip} — {needingAttention} agents need your attention",
        };
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
        _badgeTimer?.Stop();
        _quitItem.Click -= OnQuitClicked;
        Avalonia.Controls.TrayIcon.SetIcons(Avalonia.Application.Current!, null!);
    }
}
