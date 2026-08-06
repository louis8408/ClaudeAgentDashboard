namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>
/// Reads recent, human-readable content from a Claude Code session's own transcript file
/// (FR-019, R16). Strictly read-only and informational — no member of this interface writes
/// to, or otherwise interacts with, the transcript or the agent producing it.
/// </summary>
public interface ITranscriptReader
{
    /// <summary>
    /// The most recent <paramref name="maxEntries"/> renderable entries from the transcript at
    /// <paramref name="transcriptPath"/>, oldest first. Returns an empty list — never throws —
    /// if the file doesn't exist, isn't readable, or contains nothing renderable.
    /// </summary>
    IReadOnlyList<string> ReadRecentEntries(string transcriptPath, int maxEntries);
}
