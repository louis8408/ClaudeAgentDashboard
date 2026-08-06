namespace ClaudeAgentDashboard.Domain;

/// <summary>
/// The OS-level window associated with the terminal hosting an <see cref="AgentSession"/>.
/// Deliberately holds only the owning process id, not a cached native window handle:
/// Infrastructure implementations of IWindowFocuser re-resolve the actual handle fresh at
/// focus time (windows can move/change), keeping this Domain type framework-free.
/// </summary>
public sealed class TerminalWindowReference
{
    public int OwningProcessId { get; }
    public bool IsResolvable { get; private set; }

    public TerminalWindowReference(int owningProcessId)
    {
        OwningProcessId = owningProcessId;
        IsResolvable = true;
    }

    /// <summary>One-way transition: once unresolvable, a reference is never resurrected (FR-011).</summary>
    public void MarkUnresolvable() => IsResolvable = false;
}
