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
    public event Action<AgentSession>? SessionEnded;

    public MacProcessAgentWatcher()
    {
        // Initial synchronous scan so already-running agents (FR-010) are visible to
        // GetCurrentSessions() immediately, without waiting for the first timer tick,
        // and without firing SessionStarted for sessions that predate this watcher.
        Scan(raiseEvents: false);

        _timer = new Timer(_ => Scan(raiseEvents: true), state: null, PollInterval, PollInterval);
    }

    public IReadOnlyCollection<AgentSession> GetCurrentSessions() => [.. _sessions.Values];

    private void Scan(bool raiseEvents)
    {
        var matchedProcessIds = new HashSet<int>();

        foreach (var (processId, label) in QueryMatchingProcesses())
        {
            matchedProcessIds.Add(processId);

            if (_sessions.ContainsKey(processId))
            {
                continue;
            }

            // Best-effort real working directory (R15/FR-018), same rationale as the Windows
            // PEB-based resolver: `ps`'s command-line field never contains it for a bare
            // `claude` invocation. Unverified on real macOS hardware in this session, like the
            // rest of this file — degrades to null (existing label-based fallback) on any
            // failure, never throws.
            var workingDirectory = ResolveWorkingDirectory(processId);
            var session = new AgentSession(
                Guid.NewGuid(), label, DateTimeOffset.UtcNow, new TerminalWindowReference(processId), workingDirectory);

            if (_sessions.TryAdd(processId, session) && raiseEvents)
            {
                SessionStarted?.Invoke(session);
            }
        }

        foreach (var (processId, session) in _sessions)
        {
            if (session.SessionState == SessionState.Running && !matchedProcessIds.Contains(processId))
            {
                session.End(DateTimeOffset.UtcNow);
                if (raiseEvents)
                {
                    SessionEnded?.Invoke(session);
                }
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

    // A raw substring search across the whole command line false-matches on anything with
    // "claude" anywhere in its path or arguments — including the Claude Desktop app and this
    // repo's own processes when launched from a path containing "ClaudeAgentDashboard".
    // Matching only the executable's own base name, and excluding the Desktop app's .app
    // bundle path, is what actually distinguishes the Claude Code CLI.
    private static bool IsClaudeCodeSignature(string commandLine)
    {
        var executablePath = ExtractExecutablePath(commandLine);
        var executableName = Path.GetFileName(executablePath);

        return string.Equals(executableName, "claude", StringComparison.Ordinal)
            && !executablePath.Contains(".app/Contents", StringComparison.Ordinal);
    }

    private static string ExtractExecutablePath(string commandLine)
    {
        var trimmed = commandLine.TrimStart();
        var spaceIndex = trimmed.IndexOf(' ', StringComparison.Ordinal);
        return spaceIndex > 0 ? trimmed[..spaceIndex] : trimmed;
    }

    private static string DeriveLabel(string commandLine) =>
        commandLine.Length <= 80 ? commandLine : commandLine[..80];

    private static string? ResolveWorkingDirectory(int processId)
    {
        try
        {
            using var lsof = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/sbin/lsof",
                    ArgumentList = { "-a", "-d", "cwd", "-p", processId.ToString(System.Globalization.CultureInfo.InvariantCulture), "-Fn" },
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            lsof.Start();
            var output = lsof.StandardOutput.ReadToEnd();
            lsof.WaitForExit();

            // `-Fn` output has one field per line; the cwd's path line is prefixed with 'n'.
            var pathLine = Array.Find(
                output.Split('\n', StringSplitOptions.RemoveEmptyEntries), line => line.StartsWith('n'));

            return pathLine is { Length: > 1 } ? pathLine[1..] : null;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    public void Dispose() => _timer.Dispose();
}
