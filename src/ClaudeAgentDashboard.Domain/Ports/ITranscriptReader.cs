using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>
/// Reads recent, human-readable content from a Claude Code session's own transcript file
/// (FR-019, R16). Strictly read-only and informational — no member of this interface writes
/// to, or otherwise interacts with, the transcript or the agent producing it.
/// </summary>
public interface ITranscriptReader
{
    /// <summary>
    /// The most recent <paramref name="maxEntries"/> real conversational turns (role "user" or
    /// "assistant" with extractable text) from the transcript at <paramref name="transcriptPath"/>,
    /// oldest first. The transcript's other line types (hook events, attachments, metadata) are
    /// filtered out, not surfaced. Returns an empty list — never throws — if the file doesn't
    /// exist, isn't readable, or contains no such turns.
    /// </summary>
    IReadOnlyList<TranscriptEntry> ReadRecentEntries(string transcriptPath, int maxEntries);
}
