using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ClaudeAgentDashboard.Presentation;

public partial class App : Application
{
    private TrayIcon.TrayIconController? _trayIconController;

    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = CompositionRoot.Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Tray-only app (FR-001): no window at startup, and the process
            // must not exit just because no window is currently open.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _trayIconController = new TrayIcon.TrayIconController();
            desktop.Exit += (_, _) => _trayIconController?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}