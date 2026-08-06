using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Versioning;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Infrastructure.MacOS;

/// <summary>
/// Detects Claude Code CLI processes on macOS by shelling out to `ps` (research.md R3),
/// passively — no changes to Claude Code and no per-session registration required.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed class MacProcessAgentWatcher : IAgentWatcher, IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly ConcurrentDictionary<int, AgentSession> _sessions = new();
    private readonly Timer _timer;

    public event Action<AgentSession>? SessionStarted;

    // Wired up when User Story 3 (T057) extends this class with exit-polling —
    // deliberately unraised until then.
#pragma warning disable CS0067
    public event Action<AgentSession>? SessionEnded;
#pragma warning restore CS0067

    public MacProcessAgentWatcher()
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
        using var ps = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/ps",
                Arguments = "-axo pid=,command=",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        ps.Start();
        var output = ps.StandardOutput.ReadToEnd();
        ps.WaitForExit();

        foreach (var rawLine in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.TrimStart();
            var separatorIndex = line.IndexOf(' ', StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                continue;
            }

            if (!int.TryParse(line[..separatorIndex], out var processId))
            {
                continue;
            }

            var commandLine = line[(separatorIndex + 1)..];
            if (!IsClaudeCodeSignature(commandLine))
            {
                continue;
            }

            yield return (processId, DeriveLabel(commandLine));
        }
    }

    private static bool IsClaudeCodeSignature(string commandLine) =>
        commandLine.Contains("claude", StringComparison.OrdinalIgnoreCase);

    private static string DeriveLabel(string commandLine) =>
        commandLine.Length <= 80 ? commandLine : commandLine[..80];

    public void Dispose() => _timer.Dispose();
}
