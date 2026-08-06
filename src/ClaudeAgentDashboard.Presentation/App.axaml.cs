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
        var openDashboardQuery = Services.GetRequiredService<OpenDashboardQuery>();
        var showAgentCommand = Services.GetRequiredService<ShowAgentCommand>();
        var dismissAgentCommand = Services.GetRequiredService<DismissAgentCommand>();
        var viewAgentActivityQuery = Services.GetRequiredService<ViewAgentActivityQuery>();
        var window = new AgentListWindow(openDashboardQuery, showAgentCommand, dismissAgentCommand, viewAgentActivityQuery);
        window.Show();
        window.Activate();
    }
}