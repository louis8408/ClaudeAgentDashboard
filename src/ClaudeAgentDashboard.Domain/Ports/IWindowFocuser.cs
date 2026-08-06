namespace ClaudeAgentDashboard.Domain.Ports;

public enum FocusResult
{
    Focused,
    WindowNoLongerAvailable,
}

/// <summary>Brings the terminal window associated with an <see cref="AgentSession"/> to the foreground.</summary>
public interface IWindowFocuser
{
    /// <summary>
    /// Attempts to bring the referenced window to the foreground and give it input focus.
    /// MUST NOT throw for the ordinary "window was closed" case (FR-011) — that case is
    /// reported via <see cref="FocusResult.WindowNoLongerAvailable"/> instead, including
    /// when called with a reference whose <see cref="TerminalWindowReference.IsResolvable"/>
    /// is already false.
    /// </summary>
    FocusResult Focus(TerminalWindowReference reference);
}
