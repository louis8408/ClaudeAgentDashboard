using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Application.UseCases;

/// <summary>
/// Backs User Story 3's core "notify me only when it needs me" behavior: correlates an
/// incoming hook-derived <see cref="ActivitySignal"/> to its <see cref="AgentSession"/>
/// (research.md R10), folds it in, and raises exactly one attention notification per
/// genuine transition into Idle/WaitingForInput/Ended — never for Working, and never twice
/// for the same unacknowledged attention streak (FR-007, FR-007a, research.md R11). Each
/// reason is additionally gated by the user's own Settings preference for that reason.
/// </summary>
public sealed class ApplyActivitySignalCommand(AgentSessionRegistry registry, INotifier notifier, ISettingsStore settingsStore)
{
    public async Task ExecuteAsync(ActivitySignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);

        var session = registry.FindByCorrelationKey(signal.CorrelationKey);
        if (session is null)
        {
            return;
        }

        var previousSessionState = session.SessionState;
        var previousActivityState = session.ActivityState;

        if (!session.ApplySignal(signal))
        {
            return;
        }

        var reason = DetermineAttentionReason(previousSessionState, previousActivityState, session);
        if (reason is not null && IsNotificationEnabled(reason.Value))
        {
            await notifier.NotifyAttention(session, reason.Value).ConfigureAwait(false);
        }
    }

    private bool IsNotificationEnabled(AttentionReason reason) => reason switch
    {
        AttentionReason.Idle => settingsStore.NotifyOnIdle,
        AttentionReason.WaitingForInput => settingsStore.NotifyOnWaitingForInput,
        AttentionReason.Ended => settingsStore.NotifyOnEnded,
        _ => true,
    };

    private static AttentionReason? DetermineAttentionReason(
        SessionState previousSessionState, ActivityState previousActivityState, AgentSession session)
    {
        if (session.SessionState == SessionState.Ended)
        {
            return previousSessionState == SessionState.Ended ? null : AttentionReason.Ended;
        }

        var wasAttentionNeeded = IsAttentionNeeded(previousActivityState);
        var isAttentionNeeded = IsAttentionNeeded(session.ActivityState);

        if (!isAttentionNeeded || wasAttentionNeeded)
        {
            // Either still working, or flapping within an already-unacknowledged
            // attention state without an intervening Working period (FR-007a).
            return null;
        }

        return session.ActivityState == ActivityState.Idle ? AttentionReason.Idle : AttentionReason.WaitingForInput;
    }

    private static bool IsAttentionNeeded(ActivityState state) => state is ActivityState.Idle or ActivityState.WaitingForInput;
}
