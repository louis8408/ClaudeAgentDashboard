namespace ClaudeAgentDashboard.Domain;

/// <summary>
/// One rendered conversational turn from an agent's own transcript (FR-019) — a real "user"
/// or "assistant" message with text, as opposed to the transcript's other line types (hook
/// events, attachments, metadata), which <see cref="Ports.ITranscriptReader"/> filters out
/// rather than surfacing as noise.
/// </summary>
public sealed record TranscriptEntry(string Role, string Text);
