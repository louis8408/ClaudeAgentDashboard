# Quickstart: Command Center Dashboard

Manual validation for this feature's Presentation-layer scenarios, per Constitution Principle II/III (Presentation is validated via quickstart scenarios, not a fourth automated test-layer). Domain/Application/Infrastructure changes (`UsageSnapshot`, `IUsageMetricsReader`, `FleetSummaryCalculator`, `FleetMetricsHistory`, `ISettingsStore` changes) are covered by their own unit/integration/architecture tests per `tasks.md`, not here.

## Prerequisites

- Windows or macOS, this repo built in Debug (`dotnet build ClaudeAgentDashboard.sln`).
- At least one real Claude Code CLI session available to start in a terminal, with hooks already registered for this dashboard (per `001-agent-tray-dashboard`'s quickstart setup step) so activity/usage data is available — some scenarios below explicitly also cover the *no-hooks* case.

## Scenario 1 — Table replaces cards (User Story 1)

1. Start two Claude Code CLI sessions in separate terminals.
2. Run `ClaudeAgentDashboard.Presentation` and open the dashboard from the tray/menu-bar icon.
3. **Expect**: the bottom region shows a table with exactly two rows (one per session), each showing an identifying label and status — no cards, no draggable canvas.
4. Start a third session. **Expect**: a third row appears without closing/reopening the dashboard.
5. End one session (close its terminal). **Expect**: that row updates to "ended" without a restart.
6. Confirm no "choose background" button or drag affordance exists anywhere in the window.

## Scenario 2 — Fleet summary panel (User Story 2)

1. With two or more agents running (at least one with hooks set up and at least one turn completed, so usage data exists), open the dashboard.
2. **Expect**: the top panel shows a running-agent count matching the table, a total tokens-used figure, an available-context-window figure, and two trend graphs (tokens used, running-agent count).
3. Let an agent run another turn (consume more tokens). **Expect**: the figures and graphs update without reopening the dashboard.
4. Click the collapse control. **Expect**: the panel shrinks to a compact strip and the table grows to use the freed space.
5. Click expand. **Expect**: the panel returns to full figures/graphs, current as of now.
6. Restart the application. **Expect**: the panel opens in the same collapsed/expanded state left in step 4/5.

## Scenario 3 — Partial data indicator (edge case, FR-015)

1. Start one agent with hooks registered and one *without* hooks registered (or before its first turn completes, so it has no `UsageSnapshot` yet).
2. Open the dashboard. **Expect**: the summary panel's totals reflect only the agent with usage data, and the panel visibly indicates the figures may be partial (does not silently present them as complete).

## Scenario 4 — Detail overlay from table row (User Story 3)

1. With an agent actively working, click its table row (not a "Show" button).
2. **Expect**: a detail overlay opens in the same window showing its current activity, matching the content previously shown by clicking a card in `001`.
3. Let the agent's activity change (e.g., start a tool call). **Expect**: the overlay updates live.
4. Close the overlay. **Expect**: returns to the table.

## Scenario 5 — Expand detail to full window (User Story 4)

1. Open an agent's detail overlay (Scenario 4, step 1–2).
2. Click the expand control. **Expect**: the detail view fills the entire application window; table and summary panel are no longer visible.
3. Let the agent's activity change. **Expect**: the expanded view updates live, same as the standard overlay would.
4. Click restore. **Expect**: returns to the standard overlay, table visible behind it.
5. While the overlay (standard or expanded) is open for one agent, click a different agent's table row. **Expect**: the overlay switches to the newly clicked agent's detail, keeping whatever display mode (standard/expanded) was already active.

## Scenario 6 — Visual theme consistency (FR-013, SC-007)

1. Visit every screen: table (empty and populated), summary panel (collapsed and expanded), detail overlay (standard and expanded).
2. **Expect**: all use the same dark, glowing-accent, command-center visual language — no screen retains the prior light/card-desktop look from `001`.

## Scenario 7 — Removed controls are gone (SC-006)

1. Search the entire dashboard UI (all screens from Scenario 6) for any drag-to-reposition affordance or background-image picker.
2. **Expect**: none exist.
