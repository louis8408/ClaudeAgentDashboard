# Feature Specification: Agent Tray Dashboard

**Feature Branch**: `001-agent-tray-dashboard`

**Created**: 2026-08-06

**Status**: Draft

**Input**: User description: "Build ClaudeAgentDashboard: a cross-platform (Windows and macOS primarily) background desktop application, built in C#/.NET using Avalonia UI, that lets a developer monitor Claude Code agents running in terminal/CLI sessions on their machine. Core behavior: The app runs in the background with an icon in the OS system tray/menu bar (Windows system tray, macOS menu bar). Clicking the tray icon opens a small window/popover listing all currently detected running Claude Code agents (e.g. by process, session, or working directory), showing status per agent. Each running agent in the list has a 'Show' button that brings the terminal/window that agent is running in to the foreground, focused. When an agent finishes running, the app sends an OS-native notification (Windows Toast, macOS UserNotification) saying that agent has completed. Clicking that notification opens/focuses the window the agent was running in (same behavior as the 'Show' button). The app should distinguish between agents that are still running vs. finished/idle, and reflect that in the tray icon/list. Out of scope for this first spec: managing or launching agents (the app only observes/monitors already-running agents), remote/multi-machine monitoring, and Linux support (nice-to-have later, not a launch requirement)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See all running agents at a glance (Priority: P1)

A developer is running one or more Claude Code agents in separate terminal windows while doing other work. They click the app's icon in the system tray (Windows) or menu bar (macOS) and see a list of every Claude Code agent currently running on their machine, with a status indicator per agent.

**Why this priority**: This is the core value of the app — without it there is nothing to show, notify about, or navigate to. It is the minimum viable slice: a developer can already stop periodically alt-tabbing through terminals to check on long-running agents.

**Independent Test**: Start several Claude Code CLI sessions in different terminal windows, click the tray/menu-bar icon, and confirm the popover lists one entry per running agent with a "running" status, and that the list updates when a new agent is started or an existing one exits.

**Acceptance Scenarios**:

1. **Given** two Claude Code agents are running in separate terminal windows, **When** the user clicks the tray icon, **Then** a window opens listing both agents, each marked as running.
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

### User Story 3 - Get notified the moment an agent finishes (Priority: P2)

A developer starts a long-running Claude Code agent and switches to other work. When the agent finishes, the app raises an OS-native notification saying which agent completed. The developer clicks the notification, and the terminal window that agent was running in is brought to the foreground and focused — without needing to open the dashboard window first.

**Why this priority**: This turns the dashboard from something the developer has to remember to check into something that proactively tells them. It builds directly on User Stories 1 and 2 (the same "bring window to front" behavior), so it is naturally sequenced after them, but delivers the biggest reduction in "did my agent finish yet?" polling.

**Independent Test**: Start an agent, let it run to completion, and confirm a native OS notification appears identifying that agent shortly after it finishes; click the notification and confirm the originating terminal window is raised and focused, without opening the dashboard window first.

**Acceptance Scenarios**:

1. **Given** a running agent is listed in the dashboard, **When** that agent finishes, **Then** the operating system displays a native notification identifying which agent completed.
2. **Given** a completion notification is displayed, **When** the user clicks it, **Then** the terminal window that agent was running in is brought to the foreground and focused.
3. **Given** an agent finishes while the dashboard window is closed and the user is in a different application, **When** the notification appears and is clicked, **Then** the correct window still comes to the front (the dashboard window itself does not need to be open first).
4. **Given** the dashboard is open and showing an agent as "running", **When** that agent finishes, **Then** its status in the list updates to "finished" without the user needing to reopen the window.

---

### Edge Cases

- What happens when the user clicks "Show" (or a notification) for an agent whose terminal window has since been closed? The app MUST inform the user the window is no longer available rather than silently doing nothing or focusing the wrong window.
- How does the system behave if two or more agents finish at nearly the same time? Each MUST produce its own distinct, individually-clickable notification.
- What does the dashboard show immediately after the app itself is (re)started while agents are already running? Already-running agents MUST appear in the list without requiring the user to restart those agent sessions.
- How is an agent represented if its terminal window's title or working directory changes while it runs? The list entry MUST continue to refer to the same agent without duplicating or losing it.
- What happens if the operating system denies the app permission to show notifications? The app MUST still show accurate running/finished status inside its own window even if native notifications are unavailable, and MUST make the user aware that notifications are off.
- What happens when an agent process ends abnormally (crash/error) rather than completing normally? This MUST still be treated as "finished" for status and notification purposes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST run continuously in the background with a persistent icon in the OS system tray (Windows) or menu bar (macOS).
- **FR-002**: The application MUST detect Claude Code agent sessions already running on the local machine without requiring the user to start or register them through the application itself.
- **FR-003**: Clicking the tray/menu-bar icon MUST open a window listing every currently detected agent, each showing an identifying label and a running/finished status.
- **FR-004**: The application MUST detect new agents that start, and agents that finish, while it is running, and reflect those changes in the list without requiring the user to close and reopen it.
- **FR-005**: Each running agent entry in the list MUST provide a "Show" action that brings the terminal window running that agent to the foreground and gives it input focus.
- **FR-006**: The application MUST detect when a running agent finishes (whether by normal completion or abnormal termination).
- **FR-007**: When an agent finishes, the application MUST raise an OS-native notification identifying which agent completed.
- **FR-008**: Clicking a completion notification MUST bring the corresponding agent's terminal window to the foreground and focus it, equivalently to the "Show" action, without requiring the dashboard window to be opened first.
- **FR-009**: The application MUST visually distinguish running agents from finished agents, both in the list and via the tray/menu-bar icon.
- **FR-010**: The application MUST detect agents that were already running before the application itself was started, and list them as running.
- **FR-011**: If the terminal window for an agent is no longer available when the user clicks "Show" or a completion notification, the application MUST inform the user rather than failing silently.
- **FR-012**: The application MUST retain a finished agent in the list, marked as finished, until the user dismisses it or the application restarts, so a developer who was away can still see what completed.

### Key Entities

- **Agent Session**: A single detected Claude Code CLI run on the local machine. Attributes include an identifying label (derived from its working directory and/or terminal title), current status (running or finished), and start/finish times.
- **Terminal Window Reference**: The operating-system window associated with the terminal hosting an Agent Session, used to bring that window to the foreground when "Show" or a notification is activated.
- **Completion Notification**: An OS-native notification raised when an Agent Session transitions from running to finished; it carries a reference back to that Agent Session so activating it can locate and focus the correct window.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can see the full, accurate list of currently running agents within 2 seconds of clicking the tray/menu-bar icon.
- **SC-002**: Clicking "Show" brings the correct terminal window to the foreground, focused, within 1 second, with correct-window accuracy in 100% of cases where that window still exists.
- **SC-003**: A native OS notification for a finished agent appears within 5 seconds of that agent finishing.
- **SC-004**: Clicking a completion notification brings the correct terminal window to the foreground and focus in a single click, with no intermediate steps, in 100% of cases where that window still exists.
- **SC-005**: A user can tell whether any given listed agent is running or finished at a glance, without opening or clicking into that entry.
- **SC-006**: The application's background presence does not introduce any noticeable slowdown to other running applications while idle.

## Assumptions

- "Claude Code agent" refers to Claude Code CLI sessions running in a terminal on the same machine as the dashboard app; the app detects these passively (e.g., by observing running processes and their associated terminal windows) rather than requiring agents to be launched through the dashboard.
- Each agent corresponds to one terminal window/tab at a time; the common case of one Claude Code session per terminal window is what "Show" needs to resolve correctly. Multiple unrelated agents multiplexed within a single terminal window (e.g., via a terminal multiplexer) is an advanced case not required for the initial release.
- The application is expected to be configured to launch automatically at OS login, consistent with typical background tray/menu-bar utilities, so it is available to observe agents from the moment they start.
- Finished agents are cleared from the list when the user explicitly dismisses them or when the application restarts — there is no automatic time-based expiry in the initial release.
- Windows and macOS are the required platforms for the initial release; Linux support is out of scope but not precluded architecturally for the future.
- Managing or launching agents (starting, stopping, or sending input to them) is out of scope — the application only observes and surfaces already-running agents.
- Remote or multi-machine monitoring is out of scope — the application only observes agents running on the same machine it is installed on.
