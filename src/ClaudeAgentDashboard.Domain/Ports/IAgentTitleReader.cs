namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>
/// Reads an agent session's AI-generated title from its own transcript file — a separate port
/// from the other transcript-backed readers (Interface Segregation) since it serves a
/// different caller with different data.
/// </summary>
public interface IAgentTitleReader
{
    /// <summary>
    /// The title from the most recent "ai-title" line in the transcript at
    /// <paramref name="transcriptPath"/>, or null — never throws — if the file doesn't exist,
    /// isn't readable, or has no such line yet.
    /// </summary>
    string? ReadLatestTitle(string transcriptPath);
}
