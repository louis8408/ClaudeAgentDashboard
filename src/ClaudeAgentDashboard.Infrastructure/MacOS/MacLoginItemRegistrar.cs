using System.Runtime.Versioning;

namespace ClaudeAgentDashboard.Infrastructure.MacOS;

/// <summary>
/// Registers/unregisters the app as a LaunchAgent so it launches at login (spec Assumptions),
/// gated by <see cref="ClaudeAgentDashboard.Domain.Ports.ISettingsStore.LaunchAtLoginEnabled"/>.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacLoginItemRegistrar
{
    private const string Label = "com.claudeagentdashboard";

    private readonly string _launchAgentsDirectory;
    private readonly string _executablePath;

    public MacLoginItemRegistrar(string? launchAgentsDirectory = null, string? executablePath = null)
    {
        _launchAgentsDirectory = launchAgentsDirectory ?? DefaultLaunchAgentsDirectory();
        _executablePath = executablePath
            ?? Environment.ProcessPath
            ?? throw new InvalidOperationException("Could not determine the current executable path.");
    }

    private string PlistPath => Path.Combine(_launchAgentsDirectory, $"{Label}.plist");

    public bool IsEnabled() => File.Exists(PlistPath);

    public void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            if (File.Exists(PlistPath))
            {
                File.Delete(PlistPath);
            }

            return;
        }

        Directory.CreateDirectory(_launchAgentsDirectory);
        File.WriteAllText(PlistPath, BuildPlist());
    }

    private string BuildPlist() => $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
        <plist version="1.0">
        <dict>
            <key>Label</key>
            <string>{Label}</string>
            <key>ProgramArguments</key>
            <array>
                <string>{_executablePath}</string>
            </array>
            <key>RunAtLoad</key>
            <true/>
        </dict>
        </plist>
        """;

    private static string DefaultLaunchAgentsDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents");
}
