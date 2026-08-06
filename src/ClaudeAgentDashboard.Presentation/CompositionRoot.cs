using System.Runtime.Versioning;
using ClaudeAgentDashboard.Application;
using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain.Ports;
using ClaudeAgentDashboard.Infrastructure.Hooks;
using ClaudeAgentDashboard.Infrastructure.MacOS;
using ClaudeAgentDashboard.Infrastructure.Settings;
using ClaudeAgentDashboard.Infrastructure.Transcripts;
using ClaudeAgentDashboard.Infrastructure.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeAgentDashboard.Presentation;

/// <summary>
/// Carries the hook listener's actual bound address to Presentation code (e.g.
/// TrayIconController's "set up activity detection" action) without that code needing to
/// reference the Infrastructure-layer HookEventListener type directly.
/// </summary>
public sealed record HookListenerAddress(Uri Value);

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

        services.AddSingleton<HookEventListener>();
        services.AddSingleton<IAgentActivityFeed>(sp => sp.GetRequiredService<HookEventListener>());
        services.AddSingleton<IHookRegistrar, ClaudeCodeHookRegistrar>();
        services.AddSingleton(sp => new HookListenerAddress(sp.GetRequiredService<HookEventListener>().BaseAddress));

        services.AddSingleton<AgentSessionRegistry>();
        services.AddSingleton<OpenDashboardQuery>();
        services.AddSingleton<ShowAgentCommand>();
        services.AddSingleton<ApplyActivitySignalCommand>();
        services.AddSingleton<HandleNotificationActivatedCommand>();
        services.AddSingleton<DismissAgentCommand>();
        services.AddSingleton<ViewAgentActivityQuery>();
        services.AddSingleton<ITranscriptReader, JsonlTranscriptReader>();
        services.AddSingleton<ViewAgentTranscriptQuery>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();

        var provider = services.BuildServiceProvider();
        WireEventSubscriptions(provider);
        ApplyLoginItemSetting(provider);
        return provider;
    }

    /// <summary>
    /// Syncs the OS login-item registration to the persisted setting on every startup
    /// (default: disabled — see JsonSettingsStore's doc comment for why). A no-op in the
    /// common case where the setting is already off and nothing was ever registered.
    /// </summary>
    private static void ApplyLoginItemSetting(IServiceProvider provider)
    {
        var enabled = provider.GetRequiredService<ISettingsStore>().LaunchAtLoginEnabled;

        if (OperatingSystem.IsWindows())
        {
            new WindowsLoginItemRegistrar().SetEnabled(enabled);
        }
        else if (OperatingSystem.IsMacOS())
        {
            new MacLoginItemRegistrar().SetEnabled(enabled);
        }
    }

    /// <summary>
    /// Connects the Infrastructure-sourced events to the Application use cases that react
    /// to them. This is composition-root wiring, not business logic — the use cases
    /// themselves know nothing about where their inputs come from.
    /// </summary>
    private static void WireEventSubscriptions(IServiceProvider provider)
    {
        // Resolving eagerly (rather than waiting for first use) starts the listener at
        // app startup, per FR-013's setup flow.
        var hookEventListener = provider.GetRequiredService<HookEventListener>();
        var applyActivitySignal = provider.GetRequiredService<ApplyActivitySignalCommand>();
        hookEventListener.SignalReceived += signal => _ = applyActivitySignal.ExecuteAsync(signal);

        var notifier = provider.GetRequiredService<INotifier>();
        var handleNotificationActivated = provider.GetRequiredService<HandleNotificationActivatedCommand>();
        notifier.NotificationActivated += sessionId => handleNotificationActivated.Execute(sessionId);
    }

    [SupportedOSPlatform("windows")]
    private static void RegisterWindowsServices(IServiceCollection services)
    {
        services.AddSingleton<IAgentWatcher, WindowsProcessAgentWatcher>();
        services.AddSingleton<IWindowFocuser, Win32WindowFocuser>();
        services.AddSingleton<INotifier, WindowsToastNotifier>();

        // ISettingsStore -> JsonSettingsStore (Polish)
    }

    [SupportedOSPlatform("macos")]
    private static void RegisterMacServices(IServiceCollection services)
    {
        services.AddSingleton<IAgentWatcher, MacProcessAgentWatcher>();
        services.AddSingleton<IWindowFocuser, MacWindowFocuser>();
        services.AddSingleton<INotifier, MacUserNotifier>();

        // ISettingsStore -> JsonSettingsStore (Polish)
    }
}
