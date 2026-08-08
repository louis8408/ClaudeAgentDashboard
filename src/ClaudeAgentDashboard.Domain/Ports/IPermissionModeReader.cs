using ClaudeAgentDashboard.Domain;

namespace ClaudeAgentDashboard.Domain.Ports;

/// <summary>
/// Reads an agent session's current permission mode from its own transcript file — a separate
/// port from <see cref="ITranscriptReader"/>/<see cref="IUsageMetricsReader"/> (Interface
/// Segregation) since it serves a different caller with different data than either.
/// </summary>
public interface IPermissionModeReader
{
    /// <summary>
    /// The mode from the most recent "permission-mode" line in the transcript at
    /// <paramref name="transcriptPath"/>, or <see cref="PermissionMode.Unknown"/> — never
    /// throws — if the file doesn't exist, isn't readable, or has no such line yet.
    /// </summary>
    PermissionMode ReadLatestPermissionMode(string transcriptPath);
}
