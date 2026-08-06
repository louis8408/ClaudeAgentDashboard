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
    public event Action<AgentSession>? SessionEnded;

    public WindowsProcessAgentWatcher()
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

            // Resolving the real working directory (R15/FR-018) is what makes hook correlation
            // actually work for the ordinary case — the command-line-derived label alone never
            // contains it for a bare `claude` invocation. A resolution failure (unsupported
            // architecture, access denied, anything else) yields null here and correlation
            // degrades to the pre-existing label-based fallback, per the spec's edge case.
            var workingDirectory = WindowsWorkingDirectoryResolver.Resolve(processId);
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

    // A raw substring search across the whole command line false-matches on anything with
    // "claude" anywhere in its path or arguments — including the Claude Desktop app (whose
    // command lines are full of claude-branded URL schemes/flags) and this repo's own
    // processes when launched from a path containing "ClaudeAgentDashboard". Matching only
    // the executable's own base name, and excluding MSIX-packaged (WindowsApps) paths, is
    // what actually distinguishes the Claude Code CLI: Claude Desktop's executable is also
    // named claude.exe, so the name alone isn't enough to tell them apart.
    private static bool IsClaudeCodeSignature(string commandLine)
    {
        var executablePath = ExtractExecutablePath(commandLine);
        var executableName = Path.GetFileNameWithoutExtension(executablePath);

        return string.Equals(executableName, "claude", StringComparison.OrdinalIgnoreCase)
            && !executablePath.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractExecutablePath(string commandLine)
    {
        var trimmed = commandLine.TrimStart();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        if (trimmed[0] == '"')
        {
            var closingQuote = trimmed.IndexOf('"', 1);
            return closingQuote > 0 ? trimmed[1..closingQuote] : trimmed[1..];
        }

        var spaceIndex = trimmed.IndexOf(' ', StringComparison.Ordinal);
        return spaceIndex > 0 ? trimmed[..spaceIndex] : trimmed;
    }

    private static string DeriveLabel(string commandLine) =>
        commandLine.Length <= 80 ? commandLine : commandLine[..80];

    public void Dispose() => _timer.Dispose();
}
