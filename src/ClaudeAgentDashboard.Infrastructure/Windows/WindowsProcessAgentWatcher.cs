using System.Collections.Concurrent;
using System.Management;
using System.Runtime.Versioning;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.Windows;

/// <summary>
/// Detects Claude Code CLI processes on Windows via WMI (research.md R3), passively —
/// no changes to Claude Code and no per-session registration required.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsProcessAgentWatcher : IAgentWatcher, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly ConcurrentDictionary<int, AgentSession> _sessions = new();
    private readonly Timer _timer;

    public event Action<AgentSession>? SessionStarted;

    // Wired up when User Story 3 (T056) extends this class with exit-polling —
    // deliberately unraised until then.
#pragma warning disable CS0067
    public event Action<AgentSession>? SessionEnded;
#pragma warning restore CS0067

    public WindowsProcessAgentWatcher()
    {
        // Initial synchronous scan so already-running agents (FR-010) are visible to
        // GetCurrentSessions() immediately, without waiting for the first timer tick,
        // and without firing SessionStarted for sessions that predate this watcher.
        Scan(raiseStartedEvents: false);

        _timer = new Timer(_ => Scan(raiseStartedEvents: true), state: null, PollInterval, PollInterval);
    }

    public IReadOnlyCollection<AgentSession> GetCurrentSessions() => [.. _sessions.Values];

    private void Scan(bool raiseStartedEvents)
    {
        foreach (var (processId, label) in QueryMatchingProcesses())
        {
            if (_sessions.ContainsKey(processId))
            {
                continue;
            }

            var session = new AgentSession(Guid.NewGuid(), label, DateTimeOffset.UtcNow, new TerminalWindowReference(processId));

            if (_sessions.TryAdd(processId, session) && raiseStartedEvents)
            {
                SessionStarted?.Invoke(session);
            }
        }
    }

    private static IEnumerable<(int ProcessId, string Label)> QueryMatchingProcesses()
    {
        using var searcher = new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process");
        using var results = searcher.Get();

        foreach (var item in results)
        {
            using var process = item;

            if (process["CommandLine"] is not string commandLine || !IsClaudeCodeSignature(commandLine))
            {
                continue;
            }

            var processId = Convert.ToInt32(process["ProcessId"]);
            yield return (processId, DeriveLabel(commandLine));
        }
    }

    private static bool IsClaudeCodeSignature(string commandLine) =>
        commandLine.Contains("claude", StringComparison.OrdinalIgnoreCase);

    private static string DeriveLabel(string commandLine) =>
        commandLine.Length <= 80 ? commandLine : commandLine[..80];

    public void Dispose() => _timer.Dispose();
}
