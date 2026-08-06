using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Domain.UnitTests;

public class AgentSessionTests
{
    private static AgentSession CreateSession(DateTimeOffset? startedAt = null) =>
        new(Guid.NewGuid(), "my-agent", startedAt ?? DateTimeOffset.UtcNow, new TerminalWindowReference(1234));

    [Fact]
    public void Constructor_Throws_When_Label_Is_Empty()
    {
        Assert.Throws<ArgumentException>(() =>
            new AgentSession(Guid.NewGuid(), "  ", DateTimeOffset.UtcNow, new TerminalWindowReference(1234)));
    }

    [Fact]
    public void New_Session_Starts_Running_With_Unknown_Activity()
    {
        var session = CreateSession();

        Assert.Equal(SessionState.Running, session.SessionState);
        Assert.Equal(ActivityState.Unknown, session.ActivityState);
        Assert.Null(session.EndedAt);
    }

    [Fact]
    public void End_Transitions_SessionState_And_Sets_EndedAt()
    {
        var session = CreateSession();
        var endedAt = DateTimeOffset.UtcNow.AddMinutes(1);

        session.End(endedAt);

        Assert.Equal(SessionState.Ended, session.SessionState);
        Assert.Equal(endedAt, session.EndedAt);
    }

    [Fact]
    public void End_Is_Idempotent_Once_Already_Ended()
    {
        var session = CreateSession();
        var firstEndedAt = DateTimeOffset.UtcNow.AddMinutes(1);
        session.End(firstEndedAt);

        session.End(DateTimeOffset.UtcNow.AddMinutes(5));

        Assert.Equal(firstEndedAt, session.EndedAt);
    }

    [Theory]
    [InlineData(HookEvent.UserPromptSubmit, ActivityState.Working)]
    [InlineData(HookEvent.PreToolUse, ActivityState.Working)]
    [InlineData(HookEvent.Stop, ActivityState.Idle)]
    [InlineData(HookEvent.Notification, ActivityState.WaitingForInput)]
    public void ApplySignal_Maps_HookEvent_To_ActivityState(HookEvent hookEvent, ActivityState expected)
    {
        var session = CreateSession();
        var signal = new ActivitySignal("C:\\work\\agent", hookEvent, DateTimeOffset.UtcNow, "doing something");

        var applied = session.ApplySignal(signal);

        Assert.True(applied);
        Assert.Equal(expected, session.ActivityState);
        Assert.Equal("doing something", session.ActivitySummary);
    }

    [Fact]
    public void ApplySignal_With_SessionEnd_Ends_The_Session()
    {
        var session = CreateSession();
        var occurredAt = DateTimeOffset.UtcNow;
        var signal = new ActivitySignal("C:\\work\\agent", HookEvent.SessionEnd, occurredAt);

        var applied = session.ApplySignal(signal);

        Assert.True(applied);
        Assert.Equal(SessionState.Ended, session.SessionState);
        Assert.Equal(occurredAt, session.EndedAt);
    }

    [Fact]
    public void ApplySignal_Ignores_Older_Signal_Than_Current_ActivityChangedAt()
    {
        var session = CreateSession();
        var newer = DateTimeOffset.UtcNow;
        var older = newer.AddSeconds(-30);

        session.ApplySignal(new ActivitySignal("cwd", HookEvent.Stop, newer, "idle now"));
        var appliedStale = session.ApplySignal(new ActivitySignal("cwd", HookEvent.UserPromptSubmit, older, "stale working"));

        Assert.False(appliedStale);
        Assert.Equal(ActivityState.Idle, session.ActivityState);
        Assert.Equal("idle now", session.ActivitySummary);
    }

    [Fact]
    public void Constructor_Accepts_Optional_WorkingDirectory()
    {
        var session = new AgentSession(
            Guid.NewGuid(), "my-agent", DateTimeOffset.UtcNow, new TerminalWindowReference(1234),
            workingDirectory: @"C:\work\agent");

        Assert.Equal(@"C:\work\agent", session.WorkingDirectory);
    }

    [Fact]
    public void Constructor_Leaves_WorkingDirectory_Null_When_Not_Provided()
    {
        var session = CreateSession();

        Assert.Null(session.WorkingDirectory);
    }

    [Fact]
    public void ApplySignal_Sets_TranscriptPath_When_Signal_Carries_One()
    {
        var session = CreateSession();
        var signal = new ActivitySignal(
            "cwd", HookEvent.PreToolUse, DateTimeOffset.UtcNow, "doing something",
            transcriptPath: @"C:\transcripts\a.jsonl");

        session.ApplySignal(signal);

        Assert.Equal(@"C:\transcripts\a.jsonl", session.TranscriptPath);
    }

    [Fact]
    public void ApplySignal_Keeps_Existing_TranscriptPath_When_Later_Signal_Omits_It()
    {
        var session = CreateSession();
        session.ApplySignal(new ActivitySignal(
            "cwd", HookEvent.PreToolUse, DateTimeOffset.UtcNow, transcriptPath: @"C:\transcripts\a.jsonl"));

        session.ApplySignal(new ActivitySignal(
            "cwd", HookEvent.Stop, DateTimeOffset.UtcNow.AddSeconds(1)));

        Assert.Equal(@"C:\transcripts\a.jsonl", session.TranscriptPath);
    }

    [Fact]
    public void ApplySignal_Is_NoOp_Once_Session_Has_Ended()
    {
        var session = CreateSession();
        session.End(DateTimeOffset.UtcNow);

        var applied = session.ApplySignal(new ActivitySignal("cwd", HookEvent.Stop, DateTimeOffset.UtcNow.AddMinutes(1)));

        Assert.False(applied);
        Assert.Equal(ActivityState.Unknown, session.ActivityState);
    }
}
