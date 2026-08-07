# Tasks: Command Center Dashboard

**Input**: Design documents from `/specs/002-command-center-dashboard/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/domain-ports.md, quickstart.md (all present)

**Tests**: Included and mandatory for Domain/Application/Infrastructure work — Constitution Principle II (Test-First, NON-NEGOTIABLE) applies to those three layers regardless of what's "explicitly requested." Presentation-layer code (Avalonia views, composition root wiring) is intentionally **not** given per-task automated tests, per Constitution Principle III's explicit carve-out — it is validated instead by the mandatory `quickstart.md` scenarios (Polish phase, T029).

**Organization**: Tasks are grouped by user story. US1/US2/US3 are all Priority P1 in spec.md; US4 is P2. US3 has a structural dependency on US1 (a table row must exist before it can be clicked) — see Dependencies below.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1/US2/US3/US4 — omitted for Setup, Foundational, and Polish tasks
- File paths are exact and relative to the repository root

---

## Phase 1: Setup

**Purpose**: Establish a known-green baseline before touching any code.

- [X] T001 Run `dotnet build ClaudeAgentDashboard.sln` and `dotnet test` across all four test projects (`ClaudeAgentDashboard.Domain.UnitTests`, `ClaudeAgentDashboard.Application.UnitTests`, `ClaudeAgentDashboard.Infrastructure.IntegrationTests`, `ClaudeAgentDashboard.Architecture.Tests`); confirm everything is green before starting. No new project or dependency is created for this feature (research.md R4/R5 — no new NuGet packages).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared changes every later phase depends on — the settings-store contract both US1 (removal) and US2 (addition) need, and the two-region command-center shell US1's table and US2's summary panel both mount into. Doing these once here avoids two stories editing the same files.

**⚠️ CRITICAL**: No user story work may begin until this phase's checkpoint is reached.

- [X] T002 Modify `ISettingsStore` in `src/ClaudeAgentDashboard.Domain/Ports/ISettingsStore.cs`: remove `BackgroundImagePath`, `GetCardPosition(string)`, `SetCardPosition(string, CardPosition)`; add `bool SummaryPanelCollapsed { get; set; }` (data-model.md "Modified: ISettingsStore"; satisfies part of FR-004, FR-008)
- [X] T003 [P] Delete `src/ClaudeAgentDashboard.Domain/CardPosition.cs` (data-model.md "Removed entities"; depends on T002 no longer referencing it)
- [X] T004 [P] Update `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/JsonSettingsStoreTests.cs`: remove the `BackgroundImagePath`/card-position round-trip tests, add a failing test asserting `SummaryPanelCollapsed` persists across a new `JsonSettingsStore` instance pointed at the same file. Write this test **first** and confirm it fails to compile/fails before T005 (Constitution Principle II; depends on T002 for the new member to reference)
- [X] T005 Implement the `SummaryPanelCollapsed` getter/setter and remove the deleted members' implementation in `src/ClaudeAgentDashboard.Infrastructure/Settings/JsonSettingsStore.cs` (contracts/domain-ports.md "Modified: ISettingsStore"; depends on T002, T004 — makes T004's test pass)
- [X] T006 [P] Delete `src/ClaudeAgentDashboard.Presentation/Views/AgentCardView.axaml` and `src/ClaudeAgentDashboard.Presentation/Views/AgentCardView.axaml.cs` (superseded by the table — FR-002/FR-004)
- [X] T007 Restructure `src/ClaudeAgentDashboard.Presentation/Views/DesktopWindow.axaml` and `.axaml.cs`: remove the `CardCanvas`, `ChooseBackgroundButton`, `BackgroundImage`/`DefaultBackground` elements and their code-behind handlers; replace with a `Grid` exposing a collapsible top `ContentControl` region (summary panel host) and a bottom `ContentControl` region (table host); add the shared command-center visual theme as `Window.Styles`/resource entries (dark palette, glowing accent brushes, technical typography) that every later view applies via style classes (FR-001, FR-004, FR-013; depends on T002, T005, T006)

**Checkpoint**: Solution builds; `JsonSettingsStoreTests` pass; `DesktopWindow` shows the empty two-region command-center shell with no cards, drag affordance, or background picker. User story work can now begin.

---

## Phase 3: User Story 1 - Scan every agent at a glance in a table (Priority: P1)

**Goal**: Every detected `AgentSession` appears as a table row in the bottom region, replacing cards.

**Independent Test**: Start several Claude Code agents, open the dashboard, confirm one row per agent with label/status, rows add/update live, empty state shown with none running, and no drag/background controls exist anywhere (spec.md Acceptance Scenarios 1–4).

### Implementation for User Story 1

- [X] T008 [P] [US1] Create `src/ClaudeAgentDashboard.Presentation/Views/AgentTableView.axaml` and `.axaml.cs`: an `ItemsControl`-based table (research.md R4 — no `DataGrid` package) bound to an `AgentSession` collection, with a `Grid` header row (label, status) and a per-row `DataTemplate` reproducing that grid, an empty-state message, and a `RowClicked`/`AgentClicked` event raised on row click (not yet subscribed by anything — US3 wires it later)
- [X] T009 [US1] Wire `AgentTableView` into `DesktopWindow`'s bottom region host in `src/ClaudeAgentDashboard.Presentation/Views/DesktopWindow.axaml.cs`, populated from `AgentSessionRegistry`/`OpenDashboardQuery`, refreshing when a session starts, ends, or its status changes, without requiring the window to be closed and reopened (FR-002, FR-003; depends on T007, T008)

**Checkpoint**: US1 independently functional and testable (quickstart.md Scenario 1).

---

## Phase 4: User Story 2 - See fleet-wide status in a collapsible summary panel (Priority: P1)

**Goal**: Top region shows running-agent count, total tokens used, available context window, and trend graphs; collapsible and its state persists.

**Independent Test**: With multiple agents running (some with usage data, at least one without), open the dashboard, confirm the figures/graphs, collapse/expand behavior, live updates, and the partial-data indicator when usage is incomplete (spec.md Acceptance Scenarios, FR-015).

### Tests for User Story 2 ⚠️

> Write these first; confirm each fails before its implementation task.

- [X] T010 [P] [US2] Write `tests/ClaudeAgentDashboard.Domain.UnitTests/UsageSnapshotTests.cs`: construction validation (fields `>= 0`), `ContextWindowTokensAvailable = DefaultContextWindowTokens - ContextWindowTokensInUse` floored at 0 (data-model.md "New: UsageSnapshot")
- [X] T013 [P] [US2] Write `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/JsonlUsageMetricsReaderTests.cs` against real temporary transcript-file fixtures: latest cumulative usage extracted from `message.usage` blocks, `null` returned for a missing/unreadable/malformed file or one with no assistant turns yet (research.md R1; depends on T011, T012 for the port/type to compile against)
- [X] T015 [P] [US2] Write `tests/ClaudeAgentDashboard.Application.UnitTests/FleetSummaryCalculatorTests.cs`: running-agent count regardless of usage availability, totals excluding sessions with no `UsageSnapshot`, `IsPartial = true` when any running session lacks one (contracts/domain-ports.md "FleetSummaryCalculator"; depends on T011)
- [X] T017 [P] [US2] Write `tests/ClaudeAgentDashboard.Application.UnitTests/FleetMetricsHistoryTests.cs`: 120-sample cap with oldest-sample eviction, oldest-first ordering, safe concurrent `Record`/`GetHistory` (research.md R3)
- [X] T019 [P] [US2] Write `tests/ClaudeAgentDashboard.Application.UnitTests/ViewFleetSummaryQueryTests.cs`: composes `FleetSummaryCalculator` + `FleetMetricsHistory` over the current session set and each session's latest usage, returning the current snapshot plus history series (contracts/domain-ports.md UI action table; depends on T016, T018)

### Implementation for User Story 2

- [X] T011 [US2] Implement `UsageSnapshot` (incl. `DefaultContextWindowTokens = 200_000` constant, research.md R2) in `src/ClaudeAgentDashboard.Domain/UsageSnapshot.cs` (depends on T010)
- [X] T012 [P] [US2] Add `IUsageMetricsReader` port (`UsageSnapshot? TryReadLatestUsage(string transcriptPath)`) in `src/ClaudeAgentDashboard.Domain/Ports/IUsageMetricsReader.cs`
- [X] T014 [US2] Implement `JsonlUsageMetricsReader : IUsageMetricsReader` in `src/ClaudeAgentDashboard.Infrastructure/Transcripts/JsonlUsageMetricsReader.cs`, reading the same JSONL transcript file `JsonlTranscriptReader` reads (depends on T011, T013)
- [X] T016 [US2] Implement `FleetSummaryCalculator` in `src/ClaudeAgentDashboard.Application/FleetSummaryCalculator.cs` (depends on T011, T015)
- [X] T018 [US2] Implement `FleetMetricsHistory` (in-memory 120-sample ring buffer, not persisted) in `src/ClaudeAgentDashboard.Application/FleetMetricsHistory.cs` (depends on T017)
- [X] T020 [US2] Implement `ViewFleetSummaryQuery` in `src/ClaudeAgentDashboard.Application/UseCases/ViewFleetSummaryQuery.cs` (depends on T014, T016, T018, T019)
- [X] T021 [P] [US2] Create `SparklineControl` in `src/ClaudeAgentDashboard.Presentation/Views/SparklineControl.cs`: a custom-drawn `Control` (`Render(DrawingContext)`) plotting a bound `IReadOnlyList<double>` as a trend line (research.md R5 — no charting package)
- [X] T022 [US2] Create `src/ClaudeAgentDashboard.Presentation/Views/FleetSummaryPanel.axaml` and `.axaml.cs`: running-agent count, total tokens used, available context-window figures; two `SparklineControl`s (tokens used, running-agent count) bound to `ViewFleetSummaryQuery`'s history; a partial-data indicator driven by `FleetSummarySnapshot.IsPartial` (FR-015); a collapse/expand toggle (FR-007) (depends on T020, T021)
- [X] T023 [US2] Wire `FleetSummaryPanel` into `DesktopWindow`'s top region host in `src/ClaudeAgentDashboard.Presentation/Views/DesktopWindow.axaml.cs`; read `ISettingsStore.SummaryPanelCollapsed` at startup to set initial state and write it immediately on every toggle (FR-008; depends on T005, T007, T022)
- [X] T024 [US2] Register `IUsageMetricsReader → JsonlUsageMetricsReader`, `FleetSummaryCalculator`, `FleetMetricsHistory`, `ViewFleetSummaryQuery` in `src/ClaudeAgentDashboard.Presentation/CompositionRoot.cs`; start the registry-change + 30-second-timer sampling described in research.md R3 (depends on T014, T016, T018, T020)
- [X] T025 [US2] Run `tests/ClaudeAgentDashboard.Architecture.Tests/LayeringTests.cs` and confirm it still passes unchanged against the new port/types — its rules are assembly-wide (no per-type edits expected); this task is a verification run, not a code change (depends on T014, T016, T018)

**Checkpoint**: US2 independently functional — figures/graphs live, collapsible, persisted, partial-data indicator correct (quickstart.md Scenarios 2–3).

---

## Phase 5: User Story 3 - Drill into an agent's detail from the table (Priority: P1)

**Goal**: Clicking a table row opens the same in-window detail overlay content `001-agent-tray-dashboard` already defined for card clicks.

**Independent Test**: Click a table row for a working agent; overlay opens showing live activity; closing it returns to the table (spec.md Acceptance Scenarios; quickstart.md Scenario 4).

**Depends on US1**: there is no row to click until `AgentTableView` (T008/T009) exists.

### Implementation for User Story 3

- [X] T026 [US3] Subscribe `AgentTableView`'s `AgentClicked` event in `src/ClaudeAgentDashboard.Presentation/Views/DesktopWindow.axaml.cs` to open `AgentDetailOverlay` in the existing `OverlayHost`/`OverlayScrim`, populated via the existing `ViewAgentActivityQuery`/`ViewAgentTranscriptQuery` (unchanged from `001`) — the same content path card-click used to trigger, now sourced from a table row (FR-009, FR-010; depends on T008, T009)

**Checkpoint**: US3 independently functional (quickstart.md Scenario 4).

---

## Phase 6: User Story 4 - Expand an agent's detail to fill the whole window (Priority: P2)

**Goal**: The open detail overlay can expand to fill the application window and restore back, without closing.

**Independent Test**: Open a detail overlay, expand it, confirm it fills the window and still updates live, restore it, confirm return to standard size; switch to a different agent's row while open and confirm display mode is preserved (spec.md Acceptance Scenarios; quickstart.md Scenario 5).

### Implementation for User Story 4

- [X] T027 [US4] Add an `IsExpanded` state and expand/restore control to `src/ClaudeAgentDashboard.Presentation/Views/AgentDetailOverlay.axaml` and `.axaml.cs`: `.standard`/`.expanded` style classes on `OverlayChrome` swapping `Width`/`MaxHeight`/alignment between the current fixed-centered size and a parent-filling size (research.md R6, FR-011, FR-012; depends on T026)
- [X] T028 [US4] In `DesktopWindow.axaml.cs`'s row-click handling, when a different agent's row is clicked while the overlay is already open, switch its content to the newly clicked agent while preserving whichever display mode (standard/expanded) was already active (FR-014; depends on T026, T027)

**Checkpoint**: US4 independently functional (quickstart.md Scenario 5).

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation across all stories.

- [X] T029 [P] Run `specs/002-command-center-dashboard/quickstart.md` Scenarios 1–7 end-to-end against a real build — this is this feature's **mandatory** Presentation-layer validation per Constitution Principle II/III, not an optional nice-to-have. Confirms table, summary panel, partial-data indicator, detail drilldown, expand/restore, theme consistency (FR-013/SC-007), and that removed controls (SC-006) are actually gone.
- [X] T030 Run `dotnet test` across all four test projects and confirm everything is green with no regressions from `001-agent-tray-dashboard`'s existing coverage.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories.
- **US1 (Phase 3)**: Depends on Foundational only.
- **US2 (Phase 4)**: Depends on Foundational only — independent of US1 (different region, different files).
- **US3 (Phase 5)**: Depends on Foundational **and** US1 (needs `AgentTableView`'s row-click event to exist).
- **US4 (Phase 6)**: Depends on US3 (expands the overlay US3 opens).
- **Polish (Phase 7)**: Depends on all four stories being complete.

### Parallel Opportunities

- Within Foundational: T003, T004 in parallel (after T002); T006 in parallel with the T002→T005 chain.
- Once Foundational's checkpoint is reached: US1 and US2 can be built fully in parallel (no shared files) by different developers; US3 must wait on US1's T008/T009.
- Within US2: T010, T013, T015, T017, T019 (all test-first tasks) are parallel across different files once their respective dependency (T011/T012) exists; T021 (`SparklineControl`) is parallel to the Domain/Application/Infrastructure chain.

---

## Parallel Example: User Story 2

```bash
# Once T011 (UsageSnapshot) and T012 (IUsageMetricsReader) exist, these test-first tasks run together:
Task: "Write JsonlUsageMetricsReaderTests.cs in tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/"
Task: "Write FleetSummaryCalculatorTests.cs in tests/ClaudeAgentDashboard.Application.UnitTests/"
Task: "Write FleetMetricsHistoryTests.cs in tests/ClaudeAgentDashboard.Application.UnitTests/"

# SparklineControl has no Domain/Application dependency at all:
Task: "Create SparklineControl in src/ClaudeAgentDashboard.Presentation/Views/SparklineControl.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup
2. Phase 2: Foundational (critical — blocks everything)
3. Phase 3: User Story 1
4. **STOP and VALIDATE**: quickstart.md Scenario 1 — table replaces cards, no drag/background controls
5. This alone is a complete, if summary-less, command-center table view.

### Incremental Delivery

1. Setup + Foundational → shell ready, no functional regression from `001` beyond the removed background/drag controls.
2. Add US1 → table live → validate independently.
3. Add US2 → summary panel live → validate independently (fully parallel with US1 if staffed).
4. Add US3 → detail drilldown restored (this is where the app returns to full parity with `001`'s "click for detail" behavior, now via table instead of cards).
5. Add US4 → full-window expand, the one genuinely new interaction beyond `001`.
6. Phase 7: Polish — full quickstart pass + full test suite.

---

## Notes

- [P] tasks touch different files with no incomplete-task dependency between them.
- Every Domain/Application/Infrastructure implementation task has a preceding, explicitly-sequenced test-first task per Constitution Principle II — confirm each test fails before writing the implementation that makes it pass.
- No Presentation-layer task has a paired automated test — T029's quickstart run is that layer's required validation, per Constitution Principle III.
- Commit after each task or logical group, per repository convention.
