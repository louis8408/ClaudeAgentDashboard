using System.Runtime.Versioning;
using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain.Ports;
using ClaudeAgentDashboard.Infrastructure.MacOS;
using ClaudeAgentDashboard.Infrastructure.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeAgentDashboard.Presentation;

/// <summary>
/// Wires concrete Infrastructure implementations to the Domain-owned ports via DI.
/// Only this class may reference the Infrastructure layer directly (enforced by
/// ClaudeAgentDashboard.Architecture.Tests.LayeringTests).
/// </summary>
public static class CompositionRoot
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();

        if (OperatingSystem.IsWindows())
        {
            RegisterWindowsServices(services);
        }
        else if (OperatingSystem.IsMacOS())
        {
            RegisterMacServices(services);
        }

        services.AddSingleton<OpenDashboardQuery>();
        services.AddSingleton<ShowAgentCommand>();

        return services.BuildServiceProvider();
    }

    [SupportedOSPlatform("windows")]
    private static void RegisterWindowsServices(IServiceCollection services)
    {
        services.AddSingleton<IAgentWatcher, WindowsProcessAgentWatcher>();
        services.AddSingleton<IWindowFocuser, Win32WindowFocuser>();

        // INotifier -> WindowsToastNotifier (User Story 3)
        // IAgentActivityFeed / IHookRegistrar -> HookEventListener / ClaudeCodeHookRegistrar (User Story 3)
        // ISettingsStore -> JsonSettingsStore (Polish)
    }

    [SupportedOSPlatform("macos")]
    private static void RegisterMacServices(IServiceCollection services)
    {
        services.AddSingleton<IAgentWatcher, MacProcessAgentWatcher>();
        services.AddSingleton<IWindowFocuser, MacWindowFocuser>();

        // INotifier -> MacUserNotifier (User Story 3)
        // IAgentActivityFeed / IHookRegistrar -> HookEventListener / ClaudeCodeHookRegistrar (User Story 3)
        // ISettingsStore -> JsonSettingsStore (Polish)
    }
}
