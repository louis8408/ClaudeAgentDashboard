using System.Runtime.Versioning;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Infrastructure.MacOS;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

// NOTE: developed and compiled on Windows, with no macOS machine available to run this
// against — see the KNOWN GAP note on MacUserNotifier for what remains unverified
// (NotificationActivated specifically). Delivery itself should be manually confirmed by
// running the app on real macOS hardware before relying on this in production.
[SupportedOSPlatform("macos")]
public class MacUserNotifierTests
{
    [SkippableTheory]
    [InlineData(AttentionReason.Idle)]
    [InlineData(AttentionReason.WaitingForInput)]
    [InlineData(AttentionReason.Ended)]
    public async Task NotifyAttention_Delivers_A_Real_Notification_For_Each_Reason(AttentionReason reason)
    {
        Skip.IfNot(OperatingSystem.IsMacOS());

        var session = new AgentSession(Guid.NewGuid(), "integration-test-agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1));
        var notifier = new MacUserNotifier();

        var delivered = await notifier.NotifyAttention(session, reason);

        Assert.True(delivered);
    }
}
