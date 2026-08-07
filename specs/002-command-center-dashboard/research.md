# Research: Command Center Dashboard

Resolves every "NEEDS CLARIFICATION" left open by the Technical Context in `plan.md`. Each item follows Decision / Rationale / Alternatives Considered.

## R1: Source of "tokens used" and "context window available" figures

**Decision**: Add a new Domain-owned port, `IUsageMetricsReader`, with one method — `TryReadLatestUsage(string transcriptPath) -> UsageSnapshot?` — implemented in Infrastructure by parsing the same per-session JSONL transcript file `JsonlTranscriptReader` already reads (`ITranscriptReader`, FR-019). Claude Code writes a `usage` object (`input_tokens`, `output_tokens`, `cache_creation_input_tokens`, `cache_read_input_tokens`) onto each assistant-authored transcript entry. The reader takes the most recent assistant entry's `usage` block: `TokensUsed` = that entry's `output_tokens` summed cumulatively across the session's assistant entries; `ContextWindowTokensInUse` = the latest entry's `input_tokens + cache_creation_input_tokens + cache_read_input_tokens` (this is what actually occupies the model's context window on the next turn). `ContextWindowTokensAvailable` = a documented per-model constant (see R2) minus `ContextWindowTokensInUse`, floored at 0.

**Rationale**: Claude Code's own hook payloads (`HookEventListener.HookPayload`) do not carry token/usage data today — only `cwd`, `session_id`, `tool_name`, `message`, `transcript_path`. The transcript file is already the established, read-only, no-new-permissions data source for agent output (FR-019); reusing it for usage keeps the "observe, never control" boundary (spec Assumptions) intact and avoids a second file-watching/parsing pathway. A session with no hooks configured (FR-013) or no assistant turns yet simply yields no `UsageSnapshot`, which composes directly with spec FR-015 ("exclude from aggregate, mark partial").

**Alternatives considered**:
- *Add usage fields to the hook payload/wire contract*: rejected — would require a new hook event type or payload shape (a Claude Code configuration change beyond the one-time hook registration already assumed), and hooks fire on turn boundaries, not with the same per-token granularity the transcript already has.
- *Poll a separate Claude Code API/CLI command for usage*: rejected — no such local, offline-friendly surface is assumed to exist; would add a new external dependency this app doesn't otherwise have (spec Assumptions: local-machine, passive observation only).

## R2: Context-window size constant

**Decision**: Use a single documented constant, `DefaultContextWindowTokens = 200_000`, as the model context size for every session, applied uniformly regardless of which Claude model a given session is running. Stored as a Domain constant next to the new `UsageSnapshot` type, not user-configurable in this feature.

**Rationale**: Neither the hook payload nor the transcript file identifies which model a session is running with a stable, documented field the app can rely on across Claude Code versions. 200,000 tokens is the standard context window across current Claude models, matching the spec's framing of "available context window" as an at-a-glance fleet figure (Assumptions: "not a per-session budgeting or enforcement mechanism"), not a precise per-model guarantee.

**Alternatives considered**:
- *Read model identity from the transcript and look up a per-model table*: rejected as unnecessary precision for a glanceable aggregate figure — adds a maintenance burden (a model→size table that goes stale) for a number the spec explicitly scopes as informational only.

## R3: In-app trend history for the summary panel's graphs

**Decision**: A new Application-layer component, `FleetMetricsHistory`, holds an in-memory, capped ring buffer (last 120 samples) of `FleetSummarySnapshot` values. A sample is appended whenever `AgentSessionRegistry` changes (session started/ended, activity signal applied) and additionally on a 30-second timer tick so the graphs still show movement between discrete events. The buffer is not persisted — it starts empty on every application launch.

**Rationale**: The spec's Assumptions section explicitly scopes trend graphs to "application-session history... no specific retention period," so an unbounded or disk-persisted history is out of scope. 120 samples at a mixed event/30s cadence comfortably covers a multi-hour work session in bounded memory without a new persistence mechanism (which would otherwise need its own migration/versioning concerns this feature doesn't need).

**Alternatives considered**:
- *Persist history to disk (e.g., alongside settings)*: rejected — contradicts the spec's own scoping and adds storage-schema surface for a feature explicitly about the current session only.
- *Sample only on discrete events (no timer)*: rejected — a fleet that goes quiet (all agents idle, no new signals) would show a flat graph gap instead of a continuing trend line, which reads as a stalled/broken UI rather than "nothing changed."

## R4: Table rendering approach in Avalonia

**Decision**: Build the agent table from a plain `ItemsControl` bound to the current `AgentSession` collection, with a `Grid`-based column header row and a per-item `DataTemplate` reproducing the same column grid — no new NuGet dependency.

**Rationale**: `Avalonia.Controls.DataGrid` is a separate package with its own styling surface, sorting/filtering/editing machinery, and additional SonarAnalyzer surface — none of which the spec asks for (Assumptions: sorting/filtering explicitly out of scope for this release; row count is bounded to how many agents a developer runs locally at once). An `ItemsControl` reuses the same binding/update plumbing `AgentCardView` already used for the card list, minimizing new Presentation-layer machinery for what is, going by the spec's own scope, a simple bounded list.

**Alternatives considered**:
- *`Avalonia.Controls.DataGrid`*: rejected — pulls in sort/filter/edit/column-resize features that are out of scope (spec Assumptions) and a package surface with its own accessibility and theming quirks to reconcile with the new command-center visual style.

## R5: Trend graph rendering

**Decision**: A small custom `SparklineControl` (a `Control`-derived class overriding `Render(DrawingContext)`) that draws a `Polyline`-equivalent path over a bound `IReadOnlyList<double>` — no charting NuGet package.

**Rationale**: The feature needs exactly two simple trend lines (tokens used, running-agent count) with no axes, legends, zoom, or interactivity (spec: "some nice graphs," Assumptions scope them to in-session history only, no interaction requirements in any acceptance scenario). A ~50-line custom draw is smaller, has zero third-party surface for SonarCloud/licensing to track, and matches the existing codebase's preference (per `CompositionRoot.cs` and the Infrastructure layer) for depending on the OS/BCL directly over pulling in a package where a small amount of first-party code suffices.

**Alternatives considered**:
- *OxyPlot.Avalonia / LiveCharts2*: rejected — full charting libraries (axes, legends, tooltips, theming engines) for two sparklines is disproportionate footprint, and both are dependencies the Constitution's minimal-infrastructure-leakage spirit would flag as unjustified for this need.

## R6: Detail overlay's standard ↔ full-window expand/restore mechanism

**Decision**: `AgentDetailOverlay` gains a bound `IsExpanded` state toggled by a new header button. Two Avalonia style classes (`.standard` / `.expanded`) on the existing `OverlayChrome` border swap its `Width`/`MaxHeight`/`HorizontalAlignment`/`VerticalAlignment` between the current fixed centered size and `Stretch`/parent-filling — driven entirely by a `Classes` binding in code-behind, no new window or dialog is created.

**Rationale**: Spec FR-011/FR-012 require the expanded view to be "reached and exited via in-app controls" and show "the same content... differing only in size/layout" — a style-class toggle on the existing hosted `UserControl` satisfies this with the least new state, and keeps the detail view inside `DesktopWindow`'s existing `OverlayHost`/`OverlayScrim` (per spec Assumption: not a separate OS window), consistent with how the standard overlay already works today.

**Alternatives considered**:
- *Open a second `Window` for the expanded view*: rejected — spec Assumptions explicitly keep this in-window ("not a separate OS window"), matching the existing FR-014-derived behavior from `001-agent-tray-dashboard`.

## R7: Removing card position and background image state

**Decision**: Delete `CardPosition`, the `AgentCardView` card/canvas-drag code, and `ISettingsStore.BackgroundImagePath`/`GetCardPosition`/`SetCardPosition`. On first load of `002`, the existing `JsonSettingsStore`-backed settings file is read with the old fields simply ignored by the updated `ISettingsStore` implementation (no migration step, no error) — per the spec's "MAY silently discard that now-unused saved state" edge case, and the Constitution's no-shim policy for pre-release code.

**Rationale**: Matches spec FR-004 exactly, and the Constitution's explicit "no feature flags or backwards-compatibility shims for pre-release code" instruction — the codebase has no external consumers yet, so the old fields are deleted outright rather than deprecated.

**Alternatives considered**:
- *Keep the fields but stop using them ("just in case")*: rejected per Constitution ("no backwards-compatibility shims... breaking changes are made directly").

## R8: Extending `ISettingsStore` for the summary panel's collapsed/expanded state

**Decision**: Add `bool SummaryPanelCollapsed { get; set; }` to `ISettingsStore`, implemented by the existing `JsonSettingsStore` alongside `LaunchAtLoginEnabled`, using the same read/write pattern already in place.

**Rationale**: FR-008 requires this preference to persist across restarts; it is a single boolean with the exact same persistence shape (`LaunchAtLoginEnabled`) already implemented and tested (`JsonSettingsStoreTests.cs`) — no new persistence technology needed.

**Alternatives considered**:
- *A new, separate settings file/store for layout state*: rejected — unjustified complexity for one boolean when the existing settings file/port already covers this shape.
