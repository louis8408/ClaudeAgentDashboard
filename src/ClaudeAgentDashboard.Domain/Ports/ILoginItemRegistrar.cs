namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>
/// Registers/unregisters the app to launch at OS login, gated by
/// <see cref="ISettingsStore.LaunchAtLoginEnabled"/>. Lets Settings apply the toggle
/// immediately, not just persist it for the next app restart.
/// </summary>
public interface ILoginItemRegistrar
{
    void SetEnabled(bool enabled);
}
