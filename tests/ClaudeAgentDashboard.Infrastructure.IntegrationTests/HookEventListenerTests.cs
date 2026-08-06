using System.Net;
using System.Text;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Infrastructure.Hooks;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

public class HookEventListenerTests
{
    private static readonly HttpClient Client = new();

    [Theory]
    [InlineData("hooks/user-prompt-submit", HookEvent.UserPromptSubmit)]
    [InlineData("hooks/pre-tool-use", HookEvent.PreToolUse)]
    [InlineData("hooks/stop", HookEvent.Stop)]
    [InlineData("hooks/notification", HookEvent.Notification)]
    [InlineData("hooks/session-end", HookEvent.SessionEnd)]
    public async Task Listener_Parses_A_Valid_Payload_On_Each_Route(string route, HookEvent expectedHookEvent)
    {
        using var listener = new HookEventListener(preferredPort: 51900);
        var received = new TaskCompletionSource<ActivitySignal>();
        listener.SignalReceived += signal => received.TrySetResult(signal);

        var payload = """{"cwd":"C:\\work\\my-project","session_id":"abc123","tool_name":"Read","message":"Waiting for your input"}""";
        var response = await Client.PostAsync(
            new Uri(listener.BaseAddress, route),
            new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.True(response.IsSuccessStatusCode);

        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(received.Task, completed);

        var signal = await received.Task;
        Assert.Equal(expectedHookEvent, signal.HookEvent);
        Assert.Equal("C:\\work\\my-project", signal.CorrelationKey);
    }

    [Fact]
    public async Task Listener_Responds_4xx_And_Does_Not_Crash_For_A_Malformed_Payload()
    {
        using var listener = new HookEventListener(preferredPort: 51910);
        var receivedAnySignal = false;
        listener.SignalReceived += _ => receivedAnySignal = true;

        var malformedResponse = await Client.PostAsync(
            new Uri(listener.BaseAddress, "hooks/stop"),
            new StringContent("{ this is not valid json", Encoding.UTF8, "application/json"));

        Assert.False(malformedResponse.IsSuccessStatusCode);
        Assert.False(receivedAnySignal);

        // The listener must still work for a subsequent, valid request — proving the
        // malformed one didn't crash or wedge it.
        var received = new TaskCompletionSource<ActivitySignal>();
        listener.SignalReceived += signal => received.TrySetResult(signal);

        var validPayload = """{"cwd":"C:\\work\\another-project"}""";
        var validResponse = await Client.PostAsync(
            new Uri(listener.BaseAddress, "hooks/stop"),
            new StringContent(validPayload, Encoding.UTF8, "application/json"));

        Assert.True(validResponse.IsSuccessStatusCode);
        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(received.Task, completed);
    }

    [Fact]
    public async Task Listener_Parses_TranscriptPath_When_Present()
    {
        using var listener = new HookEventListener(preferredPort: 51930);
        var received = new TaskCompletionSource<ActivitySignal>();
        listener.SignalReceived += signal => received.TrySetResult(signal);

        var payload = """{"cwd":"C:\\work\\my-project","transcript_path":"C:\\transcripts\\abc123.jsonl"}""";
        var response = await Client.PostAsync(
            new Uri(listener.BaseAddress, "hooks/pre-tool-use"),
            new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.True(response.IsSuccessStatusCode);
        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(received.Task, completed);

        var signal = await received.Task;
        Assert.Equal("C:\\transcripts\\abc123.jsonl", signal.TranscriptPath);
    }

    [Fact]
    public async Task Listener_Parses_Successfully_When_TranscriptPath_Is_Absent()
    {
        using var listener = new HookEventListener(preferredPort: 51940);
        var received = new TaskCompletionSource<ActivitySignal>();
        listener.SignalReceived += signal => received.TrySetResult(signal);

        var payload = """{"cwd":"C:\\work\\my-project"}""";
        var response = await Client.PostAsync(
            new Uri(listener.BaseAddress, "hooks/stop"),
            new StringContent(payload, Encoding.UTF8, "application/json"));

        Assert.True(response.IsSuccessStatusCode);
        var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(received.Task, completed);

        var signal = await received.Task;
        Assert.Null(signal.TranscriptPath);
    }

    [Fact]
    public async Task Listener_Responds_404_For_An_Unknown_Route()
    {
        using var listener = new HookEventListener(preferredPort: 51920);

        var response = await Client.PostAsync(
            new Uri(listener.BaseAddress, "hooks/not-a-real-route"),
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
