# Feature Specification: Command Center Dashboard

**Feature Branch**: `002-command-center-dashboard`

**Created**: 2026-08-06

**Status**: Draft

**Input**: User description: "We need to change to UI, I want to go back to a more Dashboard look, lets put the agents in a table, remove the cards, app main screen can be devided, bottom part the table with the agents, top part that is colabsable can show some states, total running agents, tokens used, context window avalliable, with some nice graphs to show all of this. When you click on the agent in the table same window popup that shows the agent deatails as current when clicking on the card, but you should be able to make the agent detial that open full screen or fill the app screen. Get rid of the backgrouinf and the button not neede anymore. IOt must look more like a command center dashboard, search the we for some examples to use as reference. Use star wars and the Matrix for insparation."

**Supersedes**: This feature replaces the card-based desktop surface introduced in `001-agent-tray-dashboard` (FR-003, FR-014 through FR-017, and User Story 5) with a table-based layout described below. All other behavior from `001-agent-tray-dashboard` (detection, "Show", notifications, activity signals) is unchanged and still applies.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Scan every agent at a glance in a table (Priority: P1)

A developer opens the dashboard and sees every currently detected agent listed as a row in a single table, instead of scattered cards on a desktop surface. Each row shows the agent's identity and current status, so the developer can scan many agents quickly without dragging, arranging, or hunting for a card.

**Why this priority**: This is the core visual shift the feature exists to deliver — without it, nothing else in this spec has anywhere to live. It directly replaces the previous card list as the primary way agents are surfaced.

**Independent Test**: Start several Claude Code agents, open the dashboard, and confirm every detected agent appears as one row in a table (not a card), each showing an identifying label and current status, with no drag-and-drop repositioning available.

**Acceptance Scenarios**:

1. **Given** three agents are running, **When** the user opens the dashboard, **Then** the bottom portion of the main window shows a table with exactly three rows, one per agent, each showing its identifying label and status.
2. **Given** the table is displayed, **When** a new agent starts or an existing one's session ends, **Then** a row is added or updated accordingly without the user closing and reopening the dashboard.
3. **Given** no agents are currently running, **When** the user opens the dashboard, **Then** the table area shows an empty state indicating no agents are active.
4. **Given** the table is displayed, **When** the user looks for a way to drag a row to reposition it or set a custom background image, **Then** no such controls are present anywhere in the dashboard.

---

### User Story 2 - See fleet-wide status at a glance in a collapsible summary panel (Priority: P1)

A developer opens the dashboard and, above the agent table, sees a summary panel showing overall figures — how many agents are currently running, how many tokens have been used, and how much context window is available — along with graphs showing how these have trended. The developer can collapse this panel to reclaim screen space for the table, and expand it again later.

**Why this priority**: This is the other half of the "dashboard" reframing the feature is named for — a command-center view needs at-a-glance fleet health, not just a per-agent list. It is tied for top priority with the table because the two panels together define the new main screen.

**Independent Test**: With multiple agents running, open the dashboard and confirm the top panel shows the current running-agent count, total tokens used, and available context window, each with an accompanying trend graph; collapse the panel and confirm the table area grows to use the reclaimed space; expand it again and confirm the figures are still current.

**Acceptance Scenarios**:

1. **Given** two agents are running, **When** the user opens the dashboard, **Then** the top panel shows a running-agent count of 2, a total tokens-used figure, and an available context-window figure.
2. **Given** the top panel is expanded, **When** the user collapses it, **Then** it shrinks to a compact strip and the table below grows to occupy the freed vertical space.
3. **Given** the top panel is collapsed, **When** the user expands it again, **Then** it returns to showing the full figures and graphs, updated to the current values.
4. **Given** the dashboard is open and an agent's status or token usage changes, **When** that change occurs, **Then** the summary figures and graphs update without the user reopening the dashboard.
5. **Given** the user collapses or expands the summary panel, **When** the application is restarted, **Then** the panel opens in the same collapsed or expanded state the user last left it in.

---

### User Story 3 - Drill into an agent's detail from the table (Priority: P1)

A developer clicks an agent's row in the table and sees the same kind of detail view previously available from clicking a card — a summary of what that agent is currently doing, its recent output, and its available actions — opened as an overlay within the same dashboard window.

**Why this priority**: Losing per-agent detail when moving from cards to a table would be a regression, not a redesign. This preserves the existing detail-drilldown value (previously delivered via card click) under the new table-based navigation, so it ties with the other P1 stories that make up the new main screen.

**Independent Test**: With an agent running, click its table row and confirm a detail overlay opens in the same window showing its current activity, recent output, and available actions (at minimum "Show", and "Dismiss" once ended) — equivalent in content to the detail view previously reached by clicking a card.

**Acceptance Scenarios**:

1. **Given** an agent is actively working, **When** the user clicks that agent's table row, **Then** a detail overlay opens in the same window showing what it is currently doing.
2. **Given** the detail overlay is open, **When** the underlying agent's activity changes, **Then** the overlay updates to reflect the new activity without the user closing and reopening it.
3. **Given** the detail overlay is open, **When** the user closes it, **Then** the view returns to the agent table in the same window.

---

### User Story 4 - Expand an agent's detail to fill the whole window (Priority: P2)

A developer viewing an agent's detail overlay wants to focus on it without the table and summary panel competing for space. They expand the detail view so it fills the entire application window, review it in that focused layout, and then return it to its normal overlay size.

**Why this priority**: This builds directly on User Story 3 and adds a focus mode for closer inspection; the dashboard is fully usable with only the standard-sized overlay, so this is a step below the P1 stories.

**Independent Test**: Open an agent's detail overlay, trigger the expand-to-full-screen control, confirm the detail view fills the entire application window with the table and summary panel no longer visible, then trigger the control again (or an equivalent close/restore action) and confirm the view returns to the standard overlay over the table.

**Acceptance Scenarios**:

1. **Given** an agent's detail overlay is open at its standard size, **When** the user chooses to expand it, **Then** the detail view fills the entire application window.
2. **Given** the detail view fills the entire application window, **When** the user chooses to restore it, **Then** it returns to the standard overlay, with the agent table visible behind/around it as before.
3. **Given** the detail view fills the entire application window, **When** the underlying agent's activity changes, **Then** the fully expanded view updates to reflect the new activity, the same as it would at standard size.

---

### Edge Cases

- What happens to an agent's saved card position or the previously chosen background image from `001-agent-tray-dashboard` after this feature ships? Both are no longer meaningful once cards and the background surface are removed; the application MUST NOT surface leftover controls for either, and MAY silently discard that now-unused saved state.
- What does the table show for an agent whose finer-grained activity is unknown (hooks not set up, per `001-agent-tray-dashboard` FR-013)? The row MUST show status as unknown rather than blank or guessed, consistent with the existing behavior for cards.
- What do the tokens-used and context-window figures show for an agent that has not reported any usage data (e.g., hooks not set up for that agent, or it just started)? That agent MUST be excluded from the aggregate figures with the summary panel indicating the figures may be partial, rather than the panel showing a misleading total or failing to render.
- What happens to the trend graphs when the application has just started and has little or no history yet? The graphs MUST render with whatever history exists (even a single data point) rather than failing to display or blocking the rest of the panel from rendering.
- What happens if the user resizes the application window very small? The table MUST remain usable (e.g., via scrolling) rather than clipping rows unreadably, and the summary panel's collapsed strip MUST remain visible.
- What happens if the user expands an agent's detail to full screen and that agent's session ends while it's expanded? The expanded view MUST reflect the ended state (consistent with the existing "session ended" detail behavior) rather than closing unexpectedly or freezing on stale data.
- What happens if the user clicks a different table row while a detail overlay (standard or expanded) is already open for another agent? The overlay MUST switch to show the newly clicked agent's detail, retaining whichever display mode (standard or expanded) was already active, rather than requiring the user to close and reopen it.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The main dashboard window MUST divide its main screen into two vertically stacked regions: a summary panel above, and an agent table below.
- **FR-002**: The agent table MUST present every currently detected Agent Session (as defined in `001-agent-tray-dashboard`) as one row, showing at minimum its identifying label and current status (working, idle, waiting for input, session ended, or unknown), replacing the previous card-based presentation.
- **FR-003**: The application MUST detect new agents and ended agent sessions while running and reflect those changes as added/updated rows in the table without requiring the dashboard to be closed and reopened, consistent with the existing detection behavior in `001-agent-tray-dashboard`.
- **FR-004**: The application MUST remove the ability to drag-reposition an agent's card and the ability to select or apply a custom background image, along with any control (e.g., a "choose background" button) that previously exposed that ability, since these no longer apply to a table-based layout.
- **FR-005**: The summary panel MUST display, for the currently detected set of agents: the count of currently running agents, a total tokens-used figure, and an available context-window figure.
- **FR-006**: The summary panel MUST display a trend graph for at least tokens used and running-agent count over time, updating as new data arrives while the dashboard is open.
- **FR-007**: The summary panel MUST be collapsible and expandable by the user; collapsing it MUST reduce it to a compact strip and give the freed vertical space to the agent table below.
- **FR-008**: The application MUST persist the summary panel's collapsed/expanded state across application restarts, so it reopens the way the user last left it.
- **FR-009**: Clicking an agent's table row MUST open a detail overlay within the same dashboard window — not a separate OS window — showing that agent's current activity, recent output, and available actions, equivalent in content to the detail view previously reached from a card (per `001-agent-tray-dashboard` FR-014 and FR-019).
- **FR-010**: The detail overlay MUST update as the underlying agent's activity changes while it is open, and closing it MUST return the user to the agent table.
- **FR-011**: The detail overlay MUST offer a control to expand it to fill the entire application main window, and, once expanded, a control to restore it back to its standard overlay size.
- **FR-012**: The expanded (full-window) detail view MUST show the same content and update behavior as the standard-size overlay for the same agent, differing only in size/layout.
- **FR-013**: The application MUST use a dark, high-contrast, sci-fi command-center visual style across the summary panel, agent table, and detail overlay — evoking a mission-control / heads-up-display aesthetic (dark backgrounds, glowing/luminous accent colors, technical/monospace-leaning typography for status and data) — applied consistently rather than only to some screens.
- **FR-014**: If the user switches to a different agent's row while a detail overlay is open (standard or expanded), the application MUST update the overlay to that agent's detail, preserving the current display mode (standard or expanded).
- **FR-015**: An agent whose tokens-used or context-window figures are unavailable (e.g., activity signals not set up for it) MUST be excluded from the aggregate figures in the summary panel, and the panel MUST indicate the totals may be partial rather than presenting them as complete.

### Key Entities

- **Fleet Summary Snapshot**: A point-in-time aggregate over all currently detected Agent Sessions — running-agent count, total tokens used, total available context window — plus enough recent history to render trend graphs. Excludes agents that have not reported usage data.
- **Dashboard Layout State**: The user's persisted preference for the summary panel's collapsed/expanded state, replacing the per-agent card position and background image state from `001-agent-tray-dashboard`, which this feature removes.
- **Agent Table Row**: The table-based presentation of an existing Agent Session (see `001-agent-tray-dashboard`) — identifying label and current status — replacing that same session's previous card presentation.
- **Detail View**: The existing per-agent detail content (activity summary, recent output, actions) from `001-agent-tray-dashboard`, now reachable from a table row instead of a card, with an added standard/expanded (full-window) display mode.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can see every currently detected agent's identity and status in one table, without any drag-and-drop arranging, within 2 seconds of opening the dashboard.
- **SC-002**: A user can read fleet-wide running-agent count, total tokens used, and available context window within 1 second of opening the dashboard, without clicking into any individual agent.
- **SC-003**: Collapsing the summary panel increases the number of agent rows visible without scrolling, on the same window size, compared to when the panel is expanded.
- **SC-004**: A user can open an agent's detail from the table in a single click, expand it to fill the window in one additional click, and return to the table in one further click — no more than three total actions from table to full-window detail and back.
- **SC-005**: A user's summary-panel collapse/expand preference is exactly as they left it in 100% of cases after restarting the application.
- **SC-006**: No control for repositioning a card or selecting a background image is discoverable anywhere in the dashboard after this feature ships.
- **SC-007**: Every screen in the dashboard (summary panel, table, detail overlay, expanded detail) uses the same dark command-center visual theme — no screen retains the prior light/card-desktop appearance.

## Assumptions

- This feature is a UI-layer redesign of the existing `001-agent-tray-dashboard` application: agent detection, the "Show" action, notifications, and activity-signal collection (working/idle/waiting-for-input/ended) are unchanged and reused as-is; only how agents are presented and navigated on the main screen changes.
- "Tokens used" and "context window available" are aggregated fleet-wide figures sourced from the same per-agent activity-signal mechanism already established for finer-grained status (`001-agent-tray-dashboard` FR-013); an agent for which that setup has not been completed simply does not contribute to the aggregate (see FR-015), rather than blocking the summary panel.
- "Available context window" is presented as an aggregate/summary figure across active sessions for fleet-at-a-glance purposes; it is not a per-session budgeting or enforcement mechanism, and this feature does not add any control that limits or manages an agent's context usage.
- Trend graphs cover application-session history (since the dashboard was last started), not a long-term historical archive; no specific retention period is required for the initial release.
- The agent table's columns beyond identifying label and status (e.g., project/working directory, activity summary, start time) are left to visual design to fill out using data already defined on Agent Session in `001-agent-tray-dashboard`; no new per-agent data is required beyond what that spec already defines, aside from the tokens/context figures called out above.
- "Command center" / Star Wars / Matrix-inspired visual direction is a styling mandate (dark theme, glowing accents, technical typography, HUD-like framing) rather than a literal reproduction of any copyrighted interface, consistent with avoiding implementation- and asset-specific detail in this specification.
- Sorting, filtering, or searching the agent table is not required for the initial release; the table is expected to remain small enough (bounded by the number of agents a developer runs locally at once) that a plain scrollable list is sufficient.
- The full-window expanded detail view is reached and exited via in-app controls (not a separate OS window), consistent with the existing single-window, overlay-based detail behavior from `001-agent-tray-dashboard`.
