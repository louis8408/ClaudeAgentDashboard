# Feature Specification: Agent Tray Dashboard

**Feature Branch**: `001-agent-tray-dashboard`

**Created**: 2026-08-06

**Status**: Draft

**Input**: User description: "Build ClaudeAgentDashboard: a cross-platform (Windows and macOS primarily) background desktop application, built in C#/.NET using Avalonia UI, that lets a developer monitor Claude Code agents running in terminal/CLI sessions on their machine. Core behavior: The app runs in the background with an icon in the OS system tray/menu bar (Windows system tray, macOS menu bar). Clicking the tray icon opens a small window/popover listing all currently detected running Claude Code agents (e.g. by process, session, or working directory), showing status per agent. Each running agent in the list has a 'Show' button that brings the terminal/window that agent is running in to the foreground, focused. When an agent finishes running, the app sends an OS-native notification (Windows Toast, macOS UserNotification) saying that agent has completed. Clicking that notification opens/focuses the window the agent was running in (same behavior as the 'Show' button). The app should distinguish between agents that are still running vs. finished/idle, and reflect that in the tray icon/list. Out of scope for this first spec: managing or launching agents (the app only observes/monitors already-running agents), remote/multi-machine monitoring, and Linux support (nice-to-have later, not a launch requirement)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See all running agents at a glance (Priority: P1)

A developer is running one or more Claude Code agents in separate terminal windows while doing other work. They click the app's icon in the system tray (Windows) or menu bar (macOS) and see a list of every Claude Code agent currently running on their machine, with a status indicator per agent.

**Why this priority**: This is the core value of the app — without it there is nothing to show, notify about, or navigate to. It is the minimum viable slice: a developer can already stop periodically alt-tabbing through terminals to check on long-running agents.

**Independent Test**: Start several Claude Code CLI sessions in different terminal windows, click the tray/menu-bar icon, and confirm the popover lists one entry per running agent with a status, and that the list updates when a new agent is started or an existing one's session ends.

**Acceptance Scenarios**:

1. **Given** two Claude Code agents are running in separate terminal windows, **When** the user clicks the tray icon, **Then** a window opens listing both agents, each showing its current status.
2. **Given** the dashboard app is opened before any agent is started, **When** the user then starts a Claude Code agent in a terminal, **Then** that agent appears in the list without the user needing to restart the dashboard app.
3. **Given** no Claude Code agents are currently running, **When** the user clicks the tray icon, **Then** the window opens showing an empty state indicating no agents are active.

---

### User Story 2 - Jump straight to an agent's window (Priority: P1)

A developer sees an agent listed in the dashboard and wants to check on its progress. They click the "Show" button next to that agent, and the terminal window running that agent is brought to the front and focused, regardless of what other application currently has focus.

**Why this priority**: Listing agents is only useful if the developer can act on what they see. Jumping directly to the right window is the other half of the core value and is tied for top priority with User Story 1.

**Independent Test**: With one or more agents running, click "Show" on a specific list entry and confirm the correct terminal window is raised above all other windows and receives keyboard focus, even when that window is minimized or on a different virtual desktop/space.

**Acceptance Scenarios**:

1. **Given** an agent is running in a terminal window that is currently in the background, **When** the user clicks "Show" for that agent, **Then** that terminal window is brought to the foreground and focused.
2. **Given** an agent is running in a minimized terminal window, **When** the user clicks "Show" for that agent, **Then** the window is restored, brought to the foreground, and focused.
3. **Given** multiple agents are listed, **When** the user clicks "Show" on one specific agent, **Then** only that agent's window is raised — other listed agents' windows are left as they were.

---

### User Story 3 - Get notified only when an agent needs me (Priority: P2)

A developer starts a Claude Code agent on a task and switches to other work. While the agent is actively working, they are not interrupted. The moment the agent stops actively working — because it finished its current turn and is waiting for the next instruction, because it needs a permission decision or an answer to a question, or because the session itself has ended — the app raises an OS-native notification saying so. The developer clicks the notification, and the terminal window that agent was running in is brought to the foreground and focused, without needing to open the dashboard window first.

**Why this priority**: The dashboard is only worth glancing away from if it proactively tells the developer the moment their attention is actually needed — and stays quiet the rest of the time. Notifying on every status change (including "still working") would train the developer to ignore notifications entirely, defeating the point. It builds on User Stories 1 and 2 (the same "bring window to front" behavior), so it is naturally sequenced after them.

**Independent Test**: Start an agent and watch it work, then let it either finish a turn (go idle), ask a permission/input question, or end its session, while the dashboard window is closed and another application has focus. Confirm a native OS notification appears identifying that agent and the reason (idle / needs input / session ended) shortly after the transition, and confirm no notification was raised while it was actively working. Click the notification and confirm the originating terminal window is raised and focused, without opening the dashboard window first.

**Acceptance Scenarios**:

1. **Given** a running agent is actively working, **When** it continues working (including starting or finishing an individual tool call as part of the same turn), **Then** no notification is raised.
2. **Given** a running agent is actively working, **When** it finishes its current turn and has nothing further to do without new input, **Then** the operating system displays a native notification identifying that agent as idle.
3. **Given** a running agent is actively working, **When** it needs a permission decision or an explicit answer from the user to continue, **Then** the operating system displays a native notification identifying that agent as waiting for input.
4. **Given** a running agent's session ends (normally or abnormally), **When** that happens, **Then** the operating system displays a native notification identifying that agent's session as ended.
5. **Given** any of the notifications above is displayed, **When** the user clicks it, **Then** the terminal window that agent was running in is brought to the foreground and focused, even if the dashboard window is closed and another application currently has focus.
6. **Given** the dashboard is open and showing an agent as "working", **When** that agent goes idle, needs input, or ends, **Then** its status in the list updates accordingly without the user needing to reopen the window.
7. **Given** an agent is already idle or waiting for input and has not been acknowledged, **When** it changes between idle and waiting-for-input again without the user acting on it, **Then** the app does not raise a second, duplicate notification for the same unacknowledged attention state.

---

### User Story 4 - See what an agent is currently doing (Priority: P3)

A developer sees an agent in the list and wants more context than just its status before deciding whether to switch to it. They click that agent's entry (not the "Show" button) and see a small detail view, inside the dashboard itself, summarizing its current activity — e.g. the tool it is currently running, the question it is waiting on an answer to, or the last thing it did.

**Why this priority**: This adds context on top of the status already delivered by User Stories 1 and 3; it is valuable but the app is still fully useful without it, so it is the lowest-priority slice.

**Independent Test**: With an agent running, click its list entry and confirm a detail view opens showing a human-readable summary of its current activity, and that the summary updates as the agent's activity changes (e.g., from "running a tool" to "waiting for your input").

**Acceptance Scenarios**:

1. **Given** an agent is actively working, **When** the user clicks that agent's entry, **Then** a detail view opens showing what it is currently doing (e.g., the tool it is running).
2. **Given** an agent is waiting for input, **When** the user clicks that agent's entry, **Then** the detail view shows what it is waiting on (e.g., the question or permission request).
3. **Given** the detail view for an agent is open, **When** that agent's activity changes, **Then** the detail view updates to reflect the new activity without the user needing to reopen it.

---

### User Story 5 - Arrange and personalize the desktop (Priority: P3)

A developer who regularly runs several agents wants the dashboard to feel like their own space rather than a fixed list: they drag each agent's card to wherever makes sense to them on the dashboard surface (e.g., grouped by project), and set a background image so the dashboard is visually distinct from other windows at a glance. The next time they open the app, their layout and background are exactly as they left them.

**Why this priority**: This is personalization on top of already-functional agent visibility (User Story 1) and detail inspection (User Story 4) — the app is fully useful without it, so it is the lowest-priority slice, sequenced after the others.

**Independent Test**: With two or more agents running, drag their cards to specific positions, set a custom background image, restart the application, and confirm both the card positions and the background image are exactly as left.

**Acceptance Scenarios**:

1. **Given** the dashboard shows one or more agent cards, **When** the user drags a card to a new position on the dashboard surface, **Then** the card stays at that position and does not automatically snap back or reflow.
2. **Given** the user has repositioned a card, **When** the application is restarted and that same agent is detected again, **Then** its card appears at the position the user last left it.
3. **Given** the dashboard is showing its default background, **When** the user selects a custom background image, **Then** the dashboard surface immediately shows that image as its background.
4. **Given** a custom background has been set, **When** the application is restarted, **Then** the same background is shown without the user needing to reselect it.

---

### Edge Cases

- What happens when the user clicks "Show" (or a notification) for an agent whose terminal window has since been closed? The app MUST inform the user the window is no longer available rather than silently doing nothing or focusing the wrong window.
- How does the system behave if two or more agents need attention (go idle, need input, or end) at nearly the same time? Each MUST produce its own distinct, individually-clickable notification.
- What does the dashboard show immediately after the app itself is (re)started while agents are already running? Already-running agents MUST appear in the list without requiring the user to restart those agent sessions; their activity state (working/idle/waiting-for-input) may briefly show as unknown until the next activity signal arrives.
- How is an agent represented if its terminal window's title or working directory changes while it runs? The list entry MUST continue to refer to the same agent without duplicating or losing it.
- What happens if the operating system denies the app permission to show notifications? The app MUST still show accurate status inside its own window even if native notifications are unavailable, and MUST make the user aware that notifications are off.
- What happens when an agent process ends abnormally (crash/error) rather than completing normally? This MUST still be treated as "session ended" for status and notification purposes.
- What happens if the one-time setup step that lets the app observe an agent's working/idle/waiting-for-input activity has not been completed for a given agent? The app MUST still show that agent as present and its session as running/ended (from process observation alone), but MAY show its finer-grained activity as "unknown" rather than guessing, and MUST make the user aware that finer-grained status requires completing setup.
- What happens if activity signals arrive out of order or are delayed? The app MUST reflect the most recently *timestamped* signal as the current activity state rather than the most recently *received* one, so a delayed "working" signal cannot overwrite a newer "idle" signal.
- What happens when a newly detected agent's card has no saved position (first time seen, or its saved position predates a change in screen size)? The app MUST place it somewhere that does not exactly overlap an existing card, rather than stacking cards unusably on top of one another.
- What happens when two agents share the same identifying label (e.g., two terminals started from the same working directory)? Card-position persistence, which keys off that label, MAY have both cards default to the same saved position — this MUST NOT crash or corrupt the saved layout; the user can still drag them apart, and each drag is saved independently going forward. This follows the same identity boundary already accepted for session correlation elsewhere in this spec.
- What happens when the application cannot determine a process's actual working directory (permissions denied, process architecture mismatch, or any other resolution failure)? The application MUST fall back to whatever weaker signal is available (e.g., the process's command line) rather than crashing or leaving the agent undetected — a resulting failure to correlate an activity signal is the same, already-specified "stays unknown" outcome (FR-013/FR-018), not a new failure mode.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST run continuously in the background with a persistent icon in the OS system tray (Windows) or menu bar (macOS).
- **FR-002**: The application MUST detect Claude Code agent sessions already running on the local machine without requiring the user to start or register them through the application itself.
- **FR-003**: Clicking the tray/menu-bar icon MUST open a window presenting every currently detected agent as a card on a desktop-style surface, each card showing an identifying icon/label and its current status (working, idle, waiting for input, session ended, or unknown per FR-013).
- **FR-004**: The application MUST detect new agents that start, and agents whose sessions end, while it is running, and reflect those changes in the list without requiring the user to close and reopen it.
- **FR-005**: Each agent entry in the list MUST provide a "Show" action that brings the terminal window running that agent to the foreground and gives it input focus.
- **FR-006**: The application MUST detect, for each agent, transitions between actively working, idle (finished its current turn, no outstanding question), and waiting for input (blocked on a permission decision or an explicit answer from the user), in addition to detecting when its session ends (normally or abnormally).
- **FR-007**: When an agent's status transitions into idle, waiting-for-input, or session-ended, the application MUST raise an OS-native notification identifying that agent and the reason. The application MUST NOT raise a notification for a transition into, or continuation of, the working state.
- **FR-007a**: The application MUST NOT raise a duplicate notification for an agent that remains in an unacknowledged attention-needed state (idle or waiting-for-input) without an intervening working period.
- **FR-008**: Clicking a notification MUST bring the corresponding agent's terminal window to the foreground and focus it, equivalently to the "Show" action, without requiring the dashboard window to be opened first.
- **FR-009**: The application MUST visually distinguish an agent's working / idle / waiting-for-input / session-ended status, both in the list and via the tray/menu-bar icon.
- **FR-010**: The application MUST detect agents that were already running before the application itself was started, and list them.
- **FR-011**: If the terminal window for an agent is no longer available when the user clicks "Show" or a notification, the application MUST inform the user rather than failing silently.
- **FR-012**: The application MUST retain an agent whose session has ended in the list, marked as ended, until the user dismisses it or the application restarts, so a developer who was away can still see what happened.
- **FR-013**: Detecting an agent's working / idle / waiting-for-input activity (as distinct from whether its session is merely running or ended) MUST be based on activity signals Claude Code itself reports, which requires a one-time setup step (registering hook commands in the user's Claude Code configuration). If that setup has not been completed for a given agent, the application MUST still show that agent and whether its session is running or ended, and MUST clearly indicate that its finer-grained activity is unknown rather than guessing.
- **FR-014**: Clicking an agent's card (distinct from a separate "Show" action available from that card) MUST open a detail view as an overlay within the same dashboard window — not a separate OS window — summarizing that agent's current activity in human-readable terms (e.g., the tool it is running, or the question it is waiting on) and offering that agent's available actions (at minimum "Show" and, once its session has ended, "Dismiss"). That view MUST update as the agent's activity changes while it is open, and closing it MUST return to the card/desktop view in the same window.
- **FR-018**: Matching an incoming activity signal (FR-013) to the correct Agent Session MUST work for the common case of a session started with no extra arguments — a working-directory-based match MUST NOT depend on the working directory happening to already appear in the process's own command line, since it normally does not. When detecting the correct agent for a signal is not possible, the application MUST leave that signal unapplied (activity stays unknown, per FR-013) rather than guessing and attributing it to the wrong agent.
- **FR-019**: The detail view (FR-014) MUST offer a read-only view of the agent's recent output/transcript content where available, refreshing as the agent's activity changes while the view is open. This is strictly informational — the application MUST NOT provide any way to send input to, or otherwise control, an agent through this or any other view (see Assumptions: the application observes agents, it does not control them).
- **FR-015**: The application MUST let the user freely reposition any agent's card anywhere within the dashboard surface (e.g., via drag), independent of any other card's position.
- **FR-016**: The application MUST persist each agent's card position across application restarts, keyed by a stable per-agent identifier (e.g., its identifying label), so a card representing the same agent identity reappears where the user last left it; an agent identity never seen before MUST receive a default position that does not exactly overlap an existing card.
- **FR-017**: The application MUST let the user select a custom background image for the dashboard surface, MUST apply it immediately, and MUST persist that choice across application restarts.

### Key Entities

- **Agent Session**: A single detected Claude Code CLI run on the local machine. Attributes include an identifying label (derived from its working directory and/or terminal title), whether its session is running or ended, its finer-grained activity status (working, idle, waiting for input, or unknown — see FR-013), a human-readable summary of its current activity for the detail view, a reference to its recent output/transcript content where available (FR-019), and start/end times.
- **Terminal Window Reference**: The operating-system window associated with the terminal hosting an Agent Session, used to bring that window to the foreground when "Show" or a notification is activated.
- **Attention Notification**: An OS-native notification raised when an Agent Session transitions into idle, waiting-for-input, or ended; it carries a reference back to that Agent Session, and the reason for the notification, so activating it can locate and focus the correct window.
- **Desktop Layout**: The user's saved arrangement of the dashboard surface — each known agent identity's last card position, and the chosen background image — persisted across application restarts, independent of any single Agent Session's lifetime (a session ends and is dismissed; its identity's saved card position and the background persist regardless).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can see the full, accurate list of currently detected agents within 2 seconds of clicking the tray/menu-bar icon.
- **SC-002**: Clicking "Show" brings the correct terminal window to the foreground, focused, within 1 second, with correct-window accuracy in 100% of cases where that window still exists.
- **SC-003**: A native OS notification for an agent that goes idle, needs input, or ends appears within 5 seconds of that transition, and zero notifications are raised while an agent is merely continuing to work.
- **SC-004**: Clicking a notification brings the correct terminal window to the foreground and focus in a single click, with no intermediate steps, in 100% of cases where that window still exists.
- **SC-005**: A user can tell whether any given listed agent is working, idle, waiting for input, or has ended at a glance, without opening or clicking into that entry.
- **SC-006**: The application's background presence does not introduce any noticeable slowdown to other running applications while idle.
- **SC-007**: A user can see a human-readable summary of what a specific agent is currently doing within 2 seconds of clicking that agent's entry.
- **SC-008**: A user can drag any agent's card to a new position, restart the application, and find that card at the same position 100% of the time.
- **SC-009**: A user can set a custom background image and find it still applied after restarting the application, in a single restart, with no reselection needed.
- **SC-010**: For a Claude Code session started with the plain `claude` command (no extra arguments) — the ordinary case — an activity signal for that session is correctly attributed to it, not left unmatched, once hooks are registered (FR-013/FR-018).

## Assumptions

- "Claude Code agent" refers to Claude Code CLI sessions running in a terminal on the same machine as the dashboard app.
- Whether an agent's session is running or has ended is detected passively (by observing running processes and their associated terminal windows), requiring no changes to Claude Code itself.
- Distinguishing an agent's finer-grained activity — working, idle, or waiting for input — is not observable passively from the OS alone, since it depends on what is happening inside the session. It requires a one-time setup step where the user (or an installer on their behalf) registers hook commands in their Claude Code configuration so Claude Code itself reports these transitions to the dashboard. This is a deliberate, narrow exception to "no changes to Claude Code required": it is a one-time local configuration step, not launching or controlling agents through the dashboard, and an agent for which this step hasn't been done is still detected and listed (per FR-013).
- Each agent corresponds to one terminal window/tab at a time; the common case of one Claude Code session per terminal window is what "Show" needs to resolve correctly. Multiple unrelated agents multiplexed within a single terminal window (e.g., via a terminal multiplexer) is an advanced case not required for the initial release.
- The application is expected to be configured to launch automatically at OS login, consistent with typical background tray/menu-bar utilities, so it is available to observe agents from the moment they start.
- Agents whose session has ended are cleared from the list when the user explicitly dismisses them or when the application restarts — there is no automatic time-based expiry in the initial release.
- Windows and macOS are the required platforms for the initial release; Linux support is out of scope but not precluded architecturally for the future.
- Managing or launching agents (starting, stopping, or sending input to them) remains out of scope — the application observes and surfaces already-running agents; registering hooks (previous bullet) configures Claude Code to report activity, it does not let the dashboard control agents.
- Remote or multi-machine monitoring is out of scope — the application only observes agents running on the same machine it is installed on.
- Resolving a tracked process's actual working directory (needed for FR-018) reads that process's own memory (its Process Environment Block) rather than asking it to cooperate — this works for same-user processes without elevated privileges, which covers the supported case of a developer monitoring their own agent sessions, but MAY fail for a process running as a different user or with mismatched process architecture (32-bit vs 64-bit); such a failure degrades to the existing "activity stays unknown" outcome (FR-013), not a crash.
- Card positions are persisted by an agent's identifying label (the same label already shown in the UI, typically derived from its working directory), not by its transient OS process id or in-memory session id — those are regenerated every time an agent starts and would make "remembering where I put this agent's card" meaningless across restarts. Two agents that happen to share a label share a saved position (see Edge Cases); this is an accepted limitation, not a defect, consistent with the spec's existing "one agent per terminal window" scope.
