using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain.Ports;
using ClaudeAgentDashboard.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeAgentDashboard.Presentation;

// Fully-qualified: within the ClaudeAgentDashboard.Presentation namespace, unqualified
// "Application" resolves to the sibling ClaudeAgentDashboard.Application project namespace
// before Avalonia's Application class (C# enclosing-namespace lookup), so it must be spelled
// out here.
public partial class App : Avalonia.Application
{
    private TrayIcon.TrayIconController? _trayIconController;
    private DesktopWindow? _desktopWindow;

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
            _trayIconController = new TrayIcon.TrayIconController(
                Services.GetRequiredService<OpenDashboardQuery>(),
                Services.GetRequiredService<IHookRegistrar>(),
                Services.GetRequiredService<HookListenerAddress>().Value);
            _trayIconController.DashboardRequested += (_, _) => OpenDashboard();
            desktop.Exit += (_, _) => _trayIconController?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OpenDashboard()
    {
        if (_desktopWindow is not null)
        {
            _desktopWindow.WindowState = WindowState.Normal;
            _desktopWindow.Activate();
            return;
        }

        var openDashboardQuery = Services.GetRequiredService<OpenDashboardQuery>();
        var showAgentCommand = Services.GetRequiredService<ShowAgentCommand>();
        var dismissAgentCommand = Services.GetRequiredService<DismissAgentCommand>();
        var viewAgentActivityQuery = Services.GetRequiredService<ViewAgentActivityQuery>();
        var viewAgentTranscriptQuery = Services.GetRequiredService<ViewAgentTranscriptQuery>();
        var viewFleetSummaryQuery = Services.GetRequiredService<ViewFleetSummaryQuery>();
        var settingsStore = Services.GetRequiredService<ISettingsStore>();
        var hookRegistrar = Services.GetRequiredService<IHookRegistrar>();
        var hookListenerBaseAddress = Services.GetRequiredService<HookListenerAddress>().Value;
        _desktopWindow = new DesktopWindow(
            openDashboardQuery, showAgentCommand, dismissAgentCommand, viewAgentActivityQuery, viewAgentTranscriptQuery,
            viewFleetSummaryQuery, settingsStore, hookRegistrar, hookListenerBaseAddress);
        _desktopWindow.Closed += (_, _) => _desktopWindow = null;
        _desktopWindow.Show();
        _desktopWindow.Activate();
    }
}