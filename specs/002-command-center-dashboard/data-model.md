# Data Model: Command Center Dashboard

All types are added to (or removed from) `ClaudeAgentDashboard.Domain` unless noted. Types and fields already defined by `001-agent-tray-dashboard` (`AgentSession`, `SessionState`, `ActivityState`, `ActivitySignal`, `HookEvent`, `AttentionNotification`, `TerminalWindowReference`) are unchanged and reused as-is — see that feature's `data-model.md` for their shape.

## New: `UsageSnapshot`

A point-in-time reading of one Agent Session's token usage, derived from its transcript (research.md R1).

| Field | Type | Notes |
|---|---|---|
| `TokensUsed` | `long` | Cumulative output tokens across the session's assistant turns so far. |
| `ContextWindowTokensInUse` | `long` | Input + cache tokens occupying context as of the most recent turn. |
| `ContextWindowTokensAvailable` | `long` | `DefaultContextWindowTokens - ContextWindowTokensInUse`, floored at 0. |
| `ReadAt` | `DateTimeOffset` | When this snapshot was produced (for staleness/ordering, mirrors `ActivitySignal.OccurredAt`). |

**Validation rules**: `TokensUsed` and `ContextWindowTokensInUse` are `>= 0`; construction throws on a negative value the same way existing Domain types validate (see `AgentSession`'s constructor guard style).

**Associated constant**: `UsageSnapshot.DefaultContextWindowTokens = 200_000` (research.md R2).

## New: `IUsageMetricsReader` (Domain port)

```text
UsageSnapshot? TryReadLatestUsage(string transcriptPath)
```

Returns `null` — never throws — if the transcript doesn't exist, isn't readable, or has no assistant turns with a `usage` block yet (mirrors `ITranscriptReader.ReadRecentEntries`'s tolerant-failure contract). Implemented in Infrastructure (`Transcripts/JsonlUsageMetricsReader`, alongside the existing `JsonlTranscriptReader`).

## New: `FleetSummarySnapshot` (Application-layer, not persisted)

The aggregate figures shown in the summary panel — spec's "Fleet Summary Snapshot" entity.

| Field | Type | Notes |
|---|---|---|
| `RunningAgentCount` | `int` | Count of `AgentSession`s with `SessionState.Running`. |
| `TotalTokensUsed` | `long` | Sum of `UsageSnapshot.TokensUsed` across sessions that have a snapshot. |
| `TotalContextWindowAvailable` | `long` | Sum of `UsageSnapshot.ContextWindowTokensAvailable` across sessions that have a snapshot. |
| `IsPartial` | `bool` | `true` when at least one running session has no `UsageSnapshot` yet (FR-015) — drives the summary panel's "figures may be partial" indicator. |
| `CapturedAt` | `DateTimeOffset` | When this snapshot was computed. |

**Computed by**: a new Application service, `FleetSummaryCalculator`, taking `AgentSessionRegistry.GetAll()` plus each session's latest `UsageSnapshot` (fetched via `IUsageMetricsReader` against `AgentSession.TranscriptPath`) and folding them per the rules above. Sessions without a `TranscriptPath`, or for which `IUsageMetricsReader` returns `null`, are excluded from the two totals and set `IsPartial = true`.

## New: `FleetMetricsHistory` (Application-layer, in-memory only — research.md R3)

Not a data model type so much as a bounded collection service: holds the last 120 `FleetSummarySnapshot`s in arrival order, appended on registry-change events and a 30-second timer tick. Exposes the current buffer as `IReadOnlyList<FleetSummarySnapshot>` for the summary panel's two trend graphs (tokens used, running-agent count) to project into `SparklineControl`-ready `double` series. Cleared on application start; never written to disk (see spec Assumptions — no retention requirement).

## New: `DashboardLayoutState`

Replaces the `Desktop Layout` entity from `001-agent-tray-dashboard` (which covered card positions + background image, both removed — research.md R7).

| Field | Type | Notes |
|---|---|---|
| `SummaryPanelCollapsed` | `bool` | Persisted via `ISettingsStore.SummaryPanelCollapsed` (research.md R8); default `false` (expanded) for a never-before-seen install. |

## Modified: `ISettingsStore` (Domain port)

- **Removed**: `string? BackgroundImagePath { get; set; }`, `CardPosition? GetCardPosition(string agentLabel)`, `void SetCardPosition(string agentLabel, CardPosition position)`.
- **Removed type**: `CardPosition` (no longer referenced anywhere once the above are gone).
- **Added**: `bool SummaryPanelCollapsed { get; set; }` (same "safe to call from the UI thread without blocking perceptibly" contract as the existing `LaunchAtLoginEnabled`).
- **Unchanged**: `bool LaunchAtLoginEnabled { get; set; }`.

## Modified: Presentation-layer view model surface

Not new Domain/Application types, but the shape each existing use case must now expose to the redesigned views:

- `OpenDashboardQuery` (existing, `001`) — its result set of `AgentSession`s now backs table rows instead of card positions; no change to its own contract, only to how `DesktopWindow` consumes the result (drops per-item position lookups).
- A new read path, `ViewFleetSummaryQuery` (Application `UseCases/`), wraps `FleetSummaryCalculator` + `FleetMetricsHistory` for the summary panel — parallel in shape to the existing `ViewAgentActivityQuery`.

## Removed entities (superseded — research.md R7)

- `CardPosition` (Domain) — deleted.
- The `Desktop Layout` entity's background-image half — deleted (no successor; command-center theme is a fixed visual style, not user-selectable per spec FR-004/FR-013).
- `AgentCardView.axaml`/`.axaml.cs`, and `DesktopWindow`'s `CardCanvas`/`ChooseBackgroundButton`/`BackgroundImage` elements — deleted, replaced by the table (FR-002) and fixed command-center styling (FR-013).
