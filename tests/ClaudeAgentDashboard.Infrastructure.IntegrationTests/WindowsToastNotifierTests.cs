using System.Runtime.Versioning;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Infrastructure.Windows;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

// NOTE: This verifies real toast *delivery* (the PowerShell/WinRT round-trip actually runs
// and the OS reports notifications enabled) for each AttentionReason. It deliberately does
// NOT assert NotificationActivated firing from a real mouse click — simulating an actual
// OS-level toast click isn't practical from an automated test in this environment. That
// wiring (stdout "ACTIVATED" marker -> event) should be manually verified by clicking a
// toast raised by the running app at least once before relying on it.
[SupportedOSPlatform("windows")]
public class WindowsToastNotifierTests
{
    [SkippableTheory]
    [InlineData(AttentionReason.Idle)]
    [InlineData(AttentionReason.WaitingForInput)]
    [InlineData(AttentionReason.Ended)]
    public async Task NotifyAttention_Delivers_A_Real_Toast_For_Each_Reason(AttentionReason reason)
    {
        Skip.IfNot(OperatingSystem.IsWindows());

        var session = new AgentSession(Guid.NewGuid(), "integration-test-agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        var notifier = new WindowsToastNotifier();

        var delivered = await notifier.NotifyAttention(session, reason);

        Assert.True(delivered);
    }
}
