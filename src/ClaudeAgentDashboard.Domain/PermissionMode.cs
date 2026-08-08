namespace ClaudeAgentDashboard.Domain;

/// <summary>
/// An agent session's current permission mode, as Claude Code itself reports it in its own
/// transcript (a "permission-mode" line) — distinct from <see cref="ActivityState"/>, which
/// comes from hook signals. <see cref="Unknown"/> covers a session with no transcript yet, or
/// one whose transcript has no permission-mode line yet (mirrors <see cref="ActivityState.Unknown"/>'s
/// "not observed yet, not an error" meaning).
/// </summary>
public enum PermissionMode
{
    Unknown,
    Manual,
    AcceptEdits,
    Plan,
    Auto,
}
