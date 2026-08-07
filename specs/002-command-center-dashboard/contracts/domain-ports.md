# Domain Port Contracts — Delta for Command Center Dashboard

This feature is a UI-layer redesign of `001-agent-tray-dashboard`; most Domain ports (`IAgentWatcher`, `IAgentActivityFeed`, `IWindowFocuser`, `INotifier`, `IHookRegistrar`) are unchanged — see `specs/001-agent-tray-dashboard/contracts/domain-ports.md` for their contracts. This file documents only what this feature adds, removes, or changes. There is no new external wire contract: `hook-event-contract.md` from `001` is unchanged, since usage data is sourced from the existing transcript file (research.md R1), not a new hook payload shape.

## New: IUsageMetricsReader

Reads the most recent token-usage reading for a session from its own transcript file (research.md R1) — strictly read-only, mirroring `ITranscriptReader`'s existing contract and "observe, never control" boundary.

- `UsageSnapshot? TryReadLatestUsage(string transcriptPath)` — the latest `UsageSnapshot` derivable from the transcript at `transcriptPath`, or `null`.

**Contract**:
- MUST NOT throw for a missing, unreadable, or malformed transcript file, or one with no assistant turns yet — returns `null` (same tolerant-failure shape as `ITranscriptReader.ReadRecentEntries`).
- MUST derive `ContextWindowTokensAvailable` using the shared `UsageSnapshot.DefaultContextWindowTokens` constant (research.md R2), never a per-call/per-model override, so fleet-wide aggregation (`FleetSummaryCalculator`) stays consistent across sessions.
- Reads fresh on every call (no internal caching/tailing state) — consistent with how `JsonlTranscriptReader` already behaves for the same file.

## Modified: ISettingsStore

- **Removed**: `string? BackgroundImagePath { get; set; }`, `CardPosition? GetCardPosition(string agentLabel)`, `void SetCardPosition(string agentLabel, CardPosition position)` — no successor; card positions and background images no longer exist as concepts (FR-004, research.md R7).
- **Added**: `bool SummaryPanelCollapsed { get; set; }` — same contract shape as the existing `LaunchAtLoginEnabled`: safe to call from the UI thread without blocking perceptibly, persists immediately on write (FR-008).

## Removed: CardPosition

The value type itself is deleted (data-model.md), not merely unused — nothing in this feature's Domain layer references drag/canvas positioning.

---

## Application-layer additions

Not Domain ports (no Infrastructure implementation needed — plain in-process computation over already-available data), but part of the seam Presentation depends on:

### FleetSummaryCalculator

- `FleetSummarySnapshot Calculate(IReadOnlyCollection<AgentSession> sessions, Func<AgentSession, UsageSnapshot?> usageLookup)` — folds the current session set plus each one's latest usage (via `IUsageMetricsReader.TryReadLatestUsage(session.TranscriptPath)`, called by the caller and passed in as `usageLookup` to keep this a pure, easily-unit-tested fold — see data-model.md).

**Contract**:
- Sessions with `TranscriptPath is null` or whose `usageLookup` returns `null` are excluded from `TotalTokensUsed`/`TotalContextWindowAvailable` and force `IsPartial = true` (FR-015).
- `RunningAgentCount` counts `SessionState.Running` sessions regardless of usage-data availability (a session with unknown usage is still running and still counted).

### FleetMetricsHistory

- `void Record(FleetSummarySnapshot snapshot)` — appends to the bounded (120-sample) in-memory buffer (research.md R3), evicting the oldest sample once full.
- `IReadOnlyList<FleetSummarySnapshot> GetHistory()` — current buffer, oldest first.

**Contract**:
- MUST NOT persist to disk (spec Assumptions: session-only history).
- MUST be safe to read (`GetHistory`) concurrently with a `Record` call from a timer callback, consistent with `AgentSessionRegistry`'s existing thread-safety approach (`ConcurrentDictionary`-backed).

---

## Presentation-facing UI contract

Extends the `001` UI-action table with this feature's new/changed actions. Presentation MUST NOT reach past Application into Infrastructure directly (Constitution Principle I).

| Action | Trigger | Behavior |
|---|---|---|
| `OpenDashboard()` | Tray/menu-bar icon clicked | Opens the main window: agent table (bottom) populated from `IAgentWatcher.GetCurrentSessions()`/`AgentSessionRegistry`, summary panel (top) populated from `ViewFleetSummaryQuery` — replaces `001`'s card-canvas population (User Story 1, 2). |
| `ViewFleetSummary()` | Dashboard open; fires on every registry change + 30s timer | Recomputes `FleetSummarySnapshot` via `FleetSummaryCalculator`, records it via `FleetMetricsHistory.Record`, and pushes updated figures + graph series to the summary panel (User Story 2). |
| `ToggleSummaryPanel()` | Collapse/expand control on the summary panel | Flips the panel's expanded/collapsed visual state and writes `ISettingsStore.SummaryPanelCollapsed` immediately (FR-007, FR-008). |
| `ViewAgentActivity(agentSessionId)` | Table row clicked | Same as `001`'s card-click behavior — opens the detail overlay in the same window, standard size (User Story 3). |
| `ToggleDetailExpanded()` | Expand/restore control on the open detail overlay | Toggles the overlay between standard and full-window display modes (research.md R6) without closing/reopening it or changing which agent it shows (User Story 4, FR-011, FR-012). |
| `ShowAgent(agentSessionId)` | "Show" button on the detail overlay | Unchanged from `001`. |
| `DismissAgent(agentSessionId)` | "Dismiss" button on the detail overlay | Unchanged from `001`. |

Removed actions (no successor — FR-004): any drag-to-reposition gesture on a card, and any "choose background image" action.
