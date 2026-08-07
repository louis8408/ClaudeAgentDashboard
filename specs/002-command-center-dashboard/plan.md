# Implementation Plan: Command Center Dashboard

**Branch**: `002-command-center-dashboard` | **Date**: 2026-08-06 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-command-center-dashboard/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Replace the card-based, freely-positioned desktop surface from `001-agent-tray-dashboard` with a two-region command-center layout: a collapsible top summary panel (running-agent count, total tokens used, available context window, trend graphs) and a bottom agent table (one row per `AgentSession`, replacing cards). Clicking a row opens the same in-window detail overlay `001` already defined, now with an added standard/full-window expand toggle. Card drag positioning and the custom background image feature are removed outright. New token/context-window figures are sourced by reading the existing per-session transcript file's `usage` blocks (no new external dependency, no new wire contract) and aggregated in a new Application-layer calculator; a small in-memory ring buffer (not persisted) feeds the trend graphs. All of `001`'s detection, "Show," and notification behavior is unchanged.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS) — matches existing `net8.0` target across all four projects.

**Primary Dependencies**: Avalonia UI (existing). No new third-party package: the agent table uses a plain `ItemsControl` (research.md R4) and the trend graphs use a small custom-drawn `Control` (research.md R5) rather than `Avalonia.Controls.DataGrid` or a charting library.

**Storage**: The existing local JSON settings file behind `ISettingsStore` (`JsonSettingsStore`), extended with one new boolean (`SummaryPanelCollapsed`) and with the now-unused background-image/card-position fields removed (research.md R7, R8). Trend-graph history is in-memory only, not persisted (research.md R3).

**Testing**: xUnit 2.5.3 + `Microsoft.NET.Test.Sdk`, `coverlet.collector` for coverage, `NetArchTest.Rules` for architecture tests — all already wired per-project; this feature adds tests to the existing `ClaudeAgentDashboard.Domain.UnitTests`, `ClaudeAgentDashboard.Application.UnitTests`, `ClaudeAgentDashboard.Infrastructure.IntegrationTests`, and `ClaudeAgentDashboard.Architecture.Tests` projects rather than creating new ones.

**Target Platform**: Windows and macOS desktop, unchanged from `001`.

**Project Type**: Desktop app — existing four-project Clean Architecture solution (`Domain` → `Application` → `Infrastructure` → `Presentation`); this feature is primarily a `Presentation` rewrite with small, well-bounded `Domain`/`Application`/`Infrastructure` additions for usage-metrics reading and fleet aggregation.

**Performance Goals**: Table and summary figures visible within 2s/1s of dashboard open respectively (SC-001/SC-002) — both are in-memory reads off data already resident (`AgentSessionRegistry`, per-session transcript files already read for FR-019), so no new performance risk beyond `001`'s existing budgets.

**Constraints**: No new NuGet dependency for table rendering or charting (research.md R4, R5) — keeps the Constitution's minimal-infrastructure-leakage spirit and avoids new SonarCloud/licensing surface. Trend history is capped at 120 in-memory samples (research.md R3) — bounded memory, no unbounded growth. No migration path for the removed `BackgroundImagePath`/card-position settings fields — old values are simply ignored on next read (research.md R7), consistent with the Constitution's no-shim policy for pre-release code.

**Scale/Scope**: Same local, single-user, single-machine scale as `001` — a bounded number of concurrently running agents on one developer's machine (spec Assumptions: table sorting/filtering explicitly out of scope, plain scrollable list is sufficient).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Clean Architecture Layering | New `IUsageMetricsReader` port is Domain-owned; its implementation (`JsonlUsageMetricsReader`) lives in Infrastructure only. `FleetSummaryCalculator`/`FleetMetricsHistory` (Application) depend only on Domain types (`AgentSession`, `UsageSnapshot`) and the `IUsageMetricsReader` abstraction — never on the Infrastructure implementation directly. No Avalonia/OS type appears in Domain or Application. | PASS |
| II. Test-First | All Domain/Application/Infrastructure additions (below) get a failing test before implementation, per `tasks.md`. Presentation changes (table, summary panel, expand toggle, theme) are validated via `quickstart.md`, per the Constitution's explicit Presentation carve-out. | PASS |
| III. Three-Layer Test Coverage | `UsageSnapshot` (Domain unit), `IUsageMetricsReader`/`JsonlUsageMetricsReader` (unit + integration against a real fixture transcript file), `FleetSummaryCalculator`/`FleetMetricsHistory` (Application unit), `ISettingsStore`/`JsonSettingsStore` changes (integration, extending `JsonSettingsStoreTests.cs`). Architecture test extended to assert the new port/implementation still respect layering direction. | PASS |
| IV. SOLID | `IUsageMetricsReader` is a single-purpose, narrow port (Interface Segregation) separate from `ITranscriptReader` even though both read the same file — they serve different callers (detail overlay vs. summary panel) and may evolve independently (e.g., a future transcript-format change need not touch usage parsing). `FleetSummaryCalculator` and `FleetMetricsHistory` are separate single-responsibility classes (calculation vs. history retention), not one combined "MetricsService." | PASS |
| V. Code Quality Gate | New code goes through the same `SonarAnalyzer.CSharp` build-time analyzer and `TreatWarningsAsErrors` already configured via `Directory.Build.props` — no project-level opt-out introduced. | PASS |

No violations — Complexity Tracking section is not needed.

## Project Structure

### Documentation (this feature)

```text
specs/002-command-center-dashboard/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── domain-ports.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── ClaudeAgentDashboard.Domain/
│   ├── UsageSnapshot.cs                    # NEW (data-model.md)
│   ├── Ports/
│   │   └── IUsageMetricsReader.cs          # NEW (contracts/domain-ports.md)
│   ├── Ports/ISettingsStore.cs             # MODIFIED — remove BackgroundImagePath/
│   │                                        #   Get|SetCardPosition, add SummaryPanelCollapsed
│   └── CardPosition.cs                     # REMOVED
│
├── ClaudeAgentDashboard.Application/
│   ├── FleetSummaryCalculator.cs           # NEW (data-model.md)
│   ├── FleetMetricsHistory.cs              # NEW (data-model.md)
│   └── UseCases/
│       └── ViewFleetSummaryQuery.cs        # NEW — parallel to existing ViewAgentActivityQuery
│
├── ClaudeAgentDashboard.Infrastructure/
│   ├── Transcripts/
│   │   └── JsonlUsageMetricsReader.cs      # NEW — implements IUsageMetricsReader
│   └── Settings/JsonSettingsStore.cs       # MODIFIED — matches ISettingsStore changes above
│
└── ClaudeAgentDashboard.Presentation/
    └── Views/
        ├── DesktopWindow.axaml(.cs)        # MODIFIED — table + collapsible summary panel
        │                                    #   replace card canvas + background image
        ├── AgentTableView.axaml(.cs)        # NEW — table (ItemsControl) replacing AgentCardView
        ├── FleetSummaryPanel.axaml(.cs)     # NEW — collapsible top panel + figures
        ├── SparklineControl.cs             # NEW — custom-drawn trend graph (research.md R5)
        ├── AgentDetailOverlay.axaml(.cs)    # MODIFIED — add standard/expanded toggle
        └── AgentCardView.axaml(.cs)         # REMOVED

tests/
├── ClaudeAgentDashboard.Domain.UnitTests/
│   └── UsageSnapshotTests.cs               # NEW
├── ClaudeAgentDashboard.Application.UnitTests/
│   ├── FleetSummaryCalculatorTests.cs      # NEW
│   ├── FleetMetricsHistoryTests.cs         # NEW
│   └── ViewFleetSummaryQueryTests.cs       # NEW
├── ClaudeAgentDashboard.Infrastructure.IntegrationTests/
│   ├── JsonlUsageMetricsReaderTests.cs     # NEW
│   └── JsonSettingsStoreTests.cs           # MODIFIED — cover SummaryPanelCollapsed, drop
│                                            #   BackgroundImagePath/CardPosition coverage
└── ClaudeAgentDashboard.Architecture.Tests/
    └── LayeringTests.cs                    # MODIFIED — extend to cover IUsageMetricsReader
```

**Structure Decision**: Reuses the existing four-project Clean Architecture solution as-is (`ClaudeAgentDashboard.sln`); no new project is created. All new Domain/Application/Infrastructure code lands in the existing project for its layer, and all new test code lands in the existing corresponding test project — consistent with the Constitution's per-layer test-project split and this feature's small, additive scope (one new port, two new Application services, one new settings field, and a Presentation-layer view rewrite).

## Complexity Tracking

*No Constitution violations — this section is intentionally empty.*
