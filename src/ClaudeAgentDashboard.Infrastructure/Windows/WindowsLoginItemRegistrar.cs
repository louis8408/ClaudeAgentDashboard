using System.Runtime.Versioning;
using ClaudeAgentDashboard.Domain.Ports;
using Microsoft.Win32;

namespace ClaudeAgentDashboard.Infrastructure.Windows;

/// <summary>
/// Registers/unregisters the app under the current user's Run key so it launches at login
/// (spec Assumptions), gated by <see cref="ClaudeAgentDashboard.Domain.Ports.ISettingsStore.LaunchAtLoginEnabled"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsLoginItemRegistrar : ILoginItemRegistrar
{
    private const string DefaultRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClaudeAgentDashboard";

    private readonly string _runKeyPath;
    private readonly string _executablePath;

    public WindowsLoginItemRegistrar(string? runKeyPath = null, string? executablePath = null)
    {
        _runKeyPath = runKeyPath ?? DefaultRunKeyPath;
        _executablePath = executablePath
            ?? Environment.ProcessPath
            ?? throw new InvalidOperationException("Could not determine the current executable path.");
    }

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(_runKeyPath, writable: false);
        return key?.GetValue(ValueName) is string existing && existing == _executablePath;
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(_runKeyPath, writable: true);

        if (enabled)
        {
            key.SetValue(ValueName, _executablePath);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
