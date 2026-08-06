using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Domain.UnitTests;

public class AttentionNotificationTests
{
    [Fact]
    public void Constructor_Stores_All_Fields()
    {
        var sessionId = Guid.NewGuid();
        var raisedAt = DateTimeOffset.UtcNow;

        var notification = new AttentionNotification(sessionId, AttentionReason.WaitingForInput, raisedAt, wasDelivered: true);

        Assert.Equal(sessionId, notification.AgentSessionId);
        Assert.Equal(AttentionReason.WaitingForInput, notification.Reason);
        Assert.Equal(raisedAt, notification.RaisedAt);
        Assert.True(notification.WasDelivered);
    }

    [Fact]
    public void Constructor_Allows_WasDelivered_False_For_Denied_Permission()
    {
        var notification = new AttentionNotification(Guid.NewGuid(), AttentionReason.Ended, DateTimeOffset.UtcNow, wasDelivered: false);

        Assert.False(notification.WasDelivered);
    }
}
