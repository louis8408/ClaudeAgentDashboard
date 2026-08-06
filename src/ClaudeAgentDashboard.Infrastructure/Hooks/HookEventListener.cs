using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Hooks;

/// <summary>
/// Ingests Claude Code hook payloads over a loopback-only HTTP listener (research.md R9),
/// per the wire contract in contracts/hook-event-contract.md. A malformed/unrecognized
/// request is logged and dropped with a 4xx response — it MUST NOT crash the listener or
/// the application, per the IAgentActivityFeed contract.
/// </summary>
public sealed class HookEventListener : IAgentActivityFeed, IDisposable
{
    private const int DefaultPort = 51820;
    private const int MaxPortAttempts = 10;

    private static readonly Dictionary<string, HookEvent> RouteMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hooks/user-prompt-submit"] = HookEvent.UserPromptSubmit,
        ["hooks/pre-tool-use"] = HookEvent.PreToolUse,
        ["hooks/stop"] = HookEvent.Stop,
        ["hooks/notification"] = HookEvent.Notification,
        ["hooks/session-end"] = HookEvent.SessionEnd,
    };

    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;

    public event Action<ActivitySignal>? SignalReceived;

    public Uri BaseAddress { get; }

    public HookEventListener(int preferredPort = DefaultPort)
    {
        (_listener, BaseAddress) = BindToFirstAvailablePort(preferredPort);
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    private static (HttpListener Listener, Uri BaseAddress) BindToFirstAvailablePort(int preferredPort)
    {
        for (var attempt = 0; attempt < MaxPortAttempts; attempt++)
        {
            var port = preferredPort + attempt;
            var listener = new HttpListener();
            var baseAddress = new Uri($"http://127.0.0.1:{port}/");
            listener.Prefixes.Add(baseAddress.ToString());

            try
            {
                listener.Start();
                return (listener, baseAddress);
            }
            catch (HttpListenerException)
            {
                listener.Close();
            }
        }

        throw new InvalidOperationException($"Could not bind the hook listener to any port starting at {preferredPort}.");
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (HttpListenerException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            _ = HandleRequestAsync(context);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            var path = context.Request.Url?.AbsolutePath.Trim('/') ?? string.Empty;
            if (!RouteMap.TryGetValue(path, out var hookEvent))
            {
                await RespondAsync(context, HttpStatusCode.NotFound).ConfigureAwait(false);
                return;
            }

            HookPayload? payload;
            using (var reader = new StreamReader(context.Request.InputStream))
            {
                var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                payload = TryParse(body);
            }

            if (payload is null || string.IsNullOrWhiteSpace(payload.Cwd))
            {
                await RespondAsync(context, HttpStatusCode.BadRequest).ConfigureAwait(false);
                return;
            }

            var signal = new ActivitySignal(
                payload.Cwd, hookEvent, DateTimeOffset.UtcNow, DeriveSummary(hookEvent, payload), payload.TranscriptPath);
            SignalReceived?.Invoke(signal);

            await RespondAsync(context, HttpStatusCode.OK).ConfigureAwait(false);
        }
        catch (IOException)
        {
            // Client disconnected mid-request — nothing to respond to.
        }
        catch (JsonException)
        {
            await RespondAsync(context, HttpStatusCode.BadRequest).ConfigureAwait(false);
        }
    }

    private static HookPayload? TryParse(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<HookPayload>(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? DeriveSummary(HookEvent hookEvent, HookPayload payload) => hookEvent switch
    {
        HookEvent.PreToolUse when !string.IsNullOrWhiteSpace(payload.ToolName) => $"Running tool: {payload.ToolName}",
        HookEvent.Notification when !string.IsNullOrWhiteSpace(payload.Message) => payload.Message,
        _ => null,
    };

    private static async Task RespondAsync(HttpListenerContext context, HttpStatusCode statusCode)
    {
        context.Response.StatusCode = (int)statusCode;
        await context.Response.OutputStream.FlushAsync().ConfigureAwait(false);
        context.Response.Close();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Close();
        try
        {
            _acceptLoop.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Loop already unwound via cancellation/close — nothing more to do.
        }

        _cts.Dispose();
    }

    private sealed class HookPayload
    {
        [JsonPropertyName("cwd")]
        public string? Cwd { get; set; }

        [JsonPropertyName("session_id")]
        public string? SessionId { get; set; }

        [JsonPropertyName("tool_name")]
        public string? ToolName { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("transcript_path")]
        public string? TranscriptPath { get; set; }
    }
}
