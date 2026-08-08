using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain.Ports;
using ClaudeAgentDashboard.Presentation.Theming;
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
    private SettingsWindow? _settingsWindow;

    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = CompositionRoot.Build();

        // Must run before any Window is constructed — DynamicResource lookups throughout every
        // view resolve against Application.Current.Resources, populated here.
        ThemeResources.Apply(Services.GetRequiredService<ISettingsStore>().Theme);

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
            _trayIconController.SettingsRequested += (_, _) => OpenSettings();
            desktop.Exit += (_, _) => _trayIconController?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OpenDashboard()
    {
        if (_desktopWindow is not null)
        {
            // Show() is required (not just Activate()) to restore a window that was hidden via
            // minimize-to-tray (DesktopWindow's Closing handler calls Hide(), not Close()) — a
            // harmless no-op if it was merely minimized rather than hidden.
            _desktopWindow.Show();
            _desktopWindow.WindowState = WindowState.Normal;
            _desktopWindow.Activate();
            return;
        }

        var openDashboardQuery = Services.GetRequiredService<OpenDashboardQuery>();
        var showAgentCommand = Services.GetRequiredService<ShowAgentCommand>();
        var dismissAgentCommand = Services.GetRequiredService<DismissAgentCommand>();
        var viewAgentActivityQuery = Services.GetRequiredService<ViewAgentActivityQuery>();
        var viewAgentTranscriptQuery = Services.GetRequiredService<ViewAgentTranscriptQuery>();
        var viewAgentModeQuery = Services.GetRequiredService<ViewAgentModeQuery>();
        var viewAgentDisplayNameQuery = Services.GetRequiredService<ViewAgentDisplayNameQuery>();
        var viewFleetSummaryQuery = Services.GetRequiredService<ViewFleetSummaryQuery>();
        var settingsStore = Services.GetRequiredService<ISettingsStore>();
        var hookRegistrar = Services.GetRequiredService<IHookRegistrar>();
        var hookListenerBaseAddress = Services.GetRequiredService<HookListenerAddress>().Value;
        _desktopWindow = new DesktopWindow(
            openDashboardQuery, showAgentCommand, dismissAgentCommand, viewAgentActivityQuery, viewAgentTranscriptQuery,
            viewAgentModeQuery, viewAgentDisplayNameQuery, viewFleetSummaryQuery, settingsStore, hookRegistrar, hookListenerBaseAddress);
        _desktopWindow.Closed += (_, _) => _desktopWindow = null;
        _desktopWindow.Show();
        _desktopWindow.Activate();
    }

    private void OpenSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var settingsStore = Services.GetRequiredService<ISettingsStore>();
        var loginItemRegistrar = Services.GetService<ILoginItemRegistrar>();
        _settingsWindow = new SettingsWindow(settingsStore, loginItemRegistrar);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }
}