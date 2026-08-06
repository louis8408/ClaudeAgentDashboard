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

        return services.BuildServiceProvider();
    }

    private static void RegisterWindowsServices(IServiceCollection services)
    {
        // IAgentWatcher -> WindowsProcessAgentWatcher (User Story 1)
        // IWindowFocuser -> Win32WindowFocuser (User Story 2)
        // INotifier -> WindowsToastNotifier (User Story 3)
        // IAgentActivityFeed / IHookRegistrar -> HookEventListener / ClaudeCodeHookRegistrar (User Story 3)
        // ISettingsStore -> JsonSettingsStore (Polish)
    }

    private static void RegisterMacServices(IServiceCollection services)
    {
        // IAgentWatcher -> MacProcessAgentWatcher (User Story 1)
        // IWindowFocuser -> MacWindowFocuser (User Story 2)
        // INotifier -> MacUserNotifier (User Story 3)
        // IAgentActivityFeed / IHookRegistrar -> HookEventListener / ClaudeCodeHookRegistrar (User Story 3)
        // ISettingsStore -> JsonSettingsStore (Polish)
    }
}
