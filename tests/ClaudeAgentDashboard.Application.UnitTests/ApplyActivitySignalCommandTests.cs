using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UnitTests;

public class ApplyActivitySignalCommandTests
{
    [Fact]
    public async Task ExecuteAsync_Does_Not_Notify_On_Transition_Into_Working()
    {
        var session = CreateSession("C:\\work\\my-project");
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var notifier = new FakeNotifier();
        var command = new ApplyActivitySignalCommand(registry, notifier);

        await command.ExecuteAsync(new ActivitySignal("C:\\work\\my-project", HookEvent.UserPromptSubmit, DateTimeOffset.UtcNow));

        Assert.Equal(ActivityState.Working, session.ActivityState);
        Assert.Empty(notifier.Notifications);
    }

    [Fact]
    public async Task ExecuteAsync_Notifies_Idle_On_Transition_From_Working()
    {
        var session = CreateSession("C:\\work\\my-project");
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var notifier = new FakeNotifier();
        var command = new ApplyActivitySignalCommand(registry, notifier);
        var now = DateTimeOffset.UtcNow;

        await command.ExecuteAsync(new ActivitySignal("C:\\work\\my-project", HookEvent.PreToolUse, now));
        await command.ExecuteAsync(new ActivitySignal("C:\\work\\my-project", HookEvent.Stop, now.AddSeconds(1)));

        Assert.Equal(ActivityState.Idle, session.ActivityState);
        Assert.Single(notifier.Notifications);
        Assert.Equal((session.Id, AttentionReason.Idle), notifier.Notifications[0]);
    }

    [Fact]
    public async Task ExecuteAsync_Does_Not_Notify_Twice_For_Flapping_Between_Idle_And_WaitingForInput()
    {
        var session = CreateSession("C:\\work\\my-project");
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var notifier = new FakeNotifier();
        var command = new ApplyActivitySignalCommand(registry, notifier);
        var now = DateTimeOffset.UtcNow;

        // Working -> Idle (notify) -> WaitingForInput (no intervening Working, no 2nd notify)
        await command.ExecuteAsync(new ActivitySignal("C:\\work\\my-project", HookEvent.PreToolUse, now));
        await command.ExecuteAsync(new ActivitySignal("C:\\work\\my-project", HookEvent.Stop, now.AddSeconds(1)));
        await command.ExecuteAsync(new ActivitySignal("C:\\work\\my-project", HookEvent.Notification, now.AddSeconds(2)));

        Assert.Equal(ActivityState.WaitingForInput, session.ActivityState);
        Assert.Single(notifier.Notifications);
        Assert.Equal(AttentionReason.Idle, notifier.Notifications[0].Reason);
    }

    [Fact]
    public async Task ExecuteAsync_Notifies_Again_After_An_Intervening_Working_Period()
    {
        var session = CreateSession("C:\\work\\my-project");
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var notifier = new FakeNotifier();
        var command = new ApplyActivitySignalCommand(registry, notifier);
        var now = DateTimeOffset.UtcNow;

        await command.ExecuteAsync(new ActivitySignal("C:\\work\\my-project", HookEvent.PreToolUse, now));
        await command.ExecuteAsync(new ActivitySignal("C:\\work\\my-project", HookEvent.Stop, now.AddSeconds(1)));
        await command.ExecuteAsync(new ActivitySignal("C:\\work\\my-project", HookEvent.UserPromptSubmit, now.AddSeconds(2)));
        await command.ExecuteAsync(new ActivitySignal("C:\\work\\my-project", HookEvent.Stop, now.AddSeconds(3)));

        Assert.Equal(2, notifier.Notifications.Count);
        Assert.All(notifier.Notifications, n => Assert.Equal(AttentionReason.Idle, n.Reason));
    }

    [Fact]
    public async Task ExecuteAsync_Notifies_Ended_Once_On_SessionEnd()
    {
        var session = CreateSession("C:\\work\\my-project");
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([session]));
        var notifier = new FakeNotifier();
        var command = new ApplyActivitySignalCommand(registry, notifier);

        await command.ExecuteAsync(new ActivitySignal("C:\\work\\my-project", HookEvent.SessionEnd, DateTimeOffset.UtcNow));

        Assert.Equal(SessionState.Ended, session.SessionState);
        Assert.Single(notifier.Notifications);
        Assert.Equal(AttentionReason.Ended, notifier.Notifications[0].Reason);
    }

    [Fact]
    public async Task ExecuteAsync_Is_A_NoOp_When_No_Session_Matches_The_Correlation_Key()
    {
        var registry = new AgentSessionRegistry(new FakeAgentWatcher([]));
        var notifier = new FakeNotifier();
        var command = new ApplyActivitySignalCommand(registry, notifier);

        await command.ExecuteAsync(new ActivitySignal("C:\\unknown\\path", HookEvent.Stop, DateTimeOffset.UtcNow));

        Assert.Empty(notifier.Notifications);
    }

    private static AgentSession CreateSession(string label) =>
        new(Guid.NewGuid(), label, DateTimeOffset.UtcNow, new TerminalWindowReference(1));

    private sealed class FakeAgentWatcher(IReadOnlyCollection<AgentSession> sessions) : IAgentWatcher
    {
#pragma warning disable CS0067
        public event Action<AgentSession>? SessionStarted;
        public event Action<AgentSession>? SessionEnded;
#pragma warning restore CS0067

        public IReadOnlyCollection<AgentSession> GetCurrentSessions() => sessions;
    }

    private sealed class FakeNotifier : INotifier
    {
        public List<(Guid SessionId, AttentionReason Reason)> Notifications { get; } = [];

#pragma warning disable CS0067
        public event Action<Guid>? NotificationActivated;
#pragma warning restore CS0067

        public Task<bool> NotifyAttention(AgentSession session, AttentionReason reason)
        {
            Notifications.Add((session.Id, reason));
            return Task.FromResult(true);
        }
    }
}
