namespace ClaudeAgentDashboard.Domain;

/// <summary>
/// A position in the desktop surface's own coordinate space (not OS screen coordinates),
/// so a saved position stays valid across different monitor setups.
/// </summary>
public readonly record struct CardPosition(double X, double Y);
