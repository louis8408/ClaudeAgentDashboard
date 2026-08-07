namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>
/// Reads the most recent token-usage reading for a session from its own transcript file
/// (002-command-center-dashboard research.md R1) — strictly read-only, mirroring
/// <see cref="ITranscriptReader"/>'s "observe, never control" boundary.
/// </summary>
public interface IUsageMetricsReader
{
    /// <summary>
    /// The latest <see cref="UsageSnapshot"/> derivable from the transcript at
    /// <paramref name="transcriptPath"/>, or null — never throws — if the file doesn't exist,
    /// isn't readable, or has no assistant turns with a usage block yet.
    /// </summary>
    UsageSnapshot? TryReadLatestUsage(string transcriptPath);
}
