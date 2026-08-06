---

description: "Task list for Agent Tray Dashboard implementation"

---

# Tasks: Agent Tray Dashboard

**Input**: Design documents from `/specs/001-agent-tray-dashboard/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/domain-ports.md, quickstart.md

**Tests**: Included and sequenced test-first (write → confirm failing → implement). The project constitution
(`.specify/memory/constitution.md`, Principles II–III) makes TDD and three-layer test coverage
non-negotiable for this project, overriding the "tests are optional" default.

**Organization**: Tasks are grouped by user story (from spec.md) to enable independent implementation and
testing of each story, per Setup → Foundational → User Story 1 → User Story 2 → User Story 3 → Polish.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Every task includes an exact file path

## Path Conventions

Four-project Clean Architecture layout under `src/`, mirrored by four test projects under `tests/`, per
plan.md's Project Structure:

```text
src/ClaudeAgentDashboard.Domain/
src/ClaudeAgentDashboard.Application/
src/ClaudeAgentDashboard.Infrastructure/{Windows,MacOS,Settings}/
src/ClaudeAgentDashboard.Presentation/{TrayIcon,Views}/
tests/ClaudeAgentDashboard.Domain.UnitTests/
tests/ClaudeAgentDashboard.Application.UnitTests/
tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/
tests/ClaudeAgentDashboard.Architecture.Tests/
```

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and CI/analysis wiring, ahead of any feature code (constitution:
Setup phase precedes feature work).

- [ ] T001 Create the four `src/` projects and four `tests/` projects listed in plan.md's Project
      Structure, and update `ClaudeAgentDashboard.sln` to reference all eight, removing the placeholder
      `ClaudeAgentDashboard\ClaudeAgentDashboard.csproj` stub reference left over from initial scaffolding
- [ ] T002 [P] Initialize `src/ClaudeAgentDashboard.Domain/ClaudeAgentDashboard.Domain.csproj` as a net8.0
      class library with zero external package references
- [ ] T003 [P] Initialize `src/ClaudeAgentDashboard.Application/ClaudeAgentDashboard.Application.csproj` as
      a net8.0 class library referencing `ClaudeAgentDashboard.Domain`
- [ ] T004 [P] Initialize `src/ClaudeAgentDashboard.Infrastructure/ClaudeAgentDashboard.Infrastructure.csproj`
      as a net8.0 class library referencing `ClaudeAgentDashboard.Domain` and `ClaudeAgentDashboard.Application`
- [ ] T005 [P] Initialize `src/ClaudeAgentDashboard.Presentation/ClaudeAgentDashboard.Presentation.csproj` as
      an Avalonia net8.0 desktop application referencing all three layers, with `Avalonia` and
      `Avalonia.Desktop` package references
- [ ] T006 [P] Initialize `tests/ClaudeAgentDashboard.Domain.UnitTests`,
      `tests/ClaudeAgentDashboard.Application.UnitTests`,
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests`, and
      `tests/ClaudeAgentDashboard.Architecture.Tests` as xUnit net8.0 projects, each referencing its
      corresponding `src/` project (`Architecture.Tests` references all four `src/` projects)
- [ ] T007 Add the `NetArchTest.Rules` package to `tests/ClaudeAgentDashboard.Architecture.Tests` and the
      `coverlet.collector` package to all four test projects
- [ ] T008 [P] Add `Directory.Build.props` at the repo root wiring `SonarAnalyzer.CSharp` as a build-time
      analyzer with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` for new code
- [ ] T009 [P] Add `sonar-project.properties` at the repo root configuring the SonarCloud project key and
      `src`/`tests` paths
- [ ] T010 [P] Add `.github/workflows/ci.yml` building `ClaudeAgentDashboard.sln`, running all four test
      projects with `coverlet` coverage, and executing the SonarCloud scan

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, port interfaces, the layering rule, and the minimal app shell that every
user story builds on (constitution: Foundational phase defines Domain entities/ports before any
Application/Infrastructure implementation).

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T011 [P] Write architecture tests asserting Domain has zero outward dependencies, Application
      depends only on Domain, and Infrastructure implementations are referenced only from Presentation, in
      `tests/ClaudeAgentDashboard.Architecture.Tests/LayeringTests.cs` (these act as regression guards from
      this point forward — they pass trivially now and must keep passing as later phases add code)
- [ ] T012 [P] Write failing unit tests for `AgentSession` construction and the `Running → Finished` state
      transition, including the `FinishedAt` null/non-null invariant, in
      `tests/ClaudeAgentDashboard.Domain.UnitTests/AgentSessionTests.cs`
- [ ] T013 [P] Write failing unit tests for `TerminalWindowReference`'s one-way `IsResolvable` transition
      in `tests/ClaudeAgentDashboard.Domain.UnitTests/TerminalWindowReferenceTests.cs`
- [ ] T014 [P] Write failing unit tests for `CompletionNotification` construction and its link back to an
      `AgentSession` id in `tests/ClaudeAgentDashboard.Domain.UnitTests/CompletionNotificationTests.cs`
- [ ] T015 [P] Implement the `AgentStatus` enum (`Running`, `Finished`) in
      `src/ClaudeAgentDashboard.Domain/AgentStatus.cs`
- [ ] T016 Implement the `AgentSession` entity in `src/ClaudeAgentDashboard.Domain/AgentSession.cs`, making
      T012 pass (depends on T015)
- [ ] T017 [P] Implement the `TerminalWindowReference` entity in
      `src/ClaudeAgentDashboard.Domain/TerminalWindowReference.cs`, making T013 pass
- [ ] T018 [P] Implement the `CompletionNotification` entity in
      `src/ClaudeAgentDashboard.Domain/CompletionNotification.cs`, making T014 pass (depends on T016)
- [ ] T019 [P] Define the `IAgentWatcher` port (`GetCurrentSessions`, `SessionStarted`, `SessionFinished`)
      in `src/ClaudeAgentDashboard.Domain/Ports/IAgentWatcher.cs`
- [ ] T020 [P] Define the `IWindowFocuser` port and `FocusResult` type in
      `src/ClaudeAgentDashboard.Domain/Ports/IWindowFocuser.cs`
- [ ] T021 [P] Define the `INotifier` port (`NotifyFinished`, `NotificationActivated`) in
      `src/ClaudeAgentDashboard.Domain/Ports/INotifier.cs`
- [ ] T022 [P] Define the `ISettingsStore` port in `src/ClaudeAgentDashboard.Domain/Ports/ISettingsStore.cs`
- [ ] T023 Implement the Avalonia application shell and composition root —
      `src/ClaudeAgentDashboard.Presentation/Program.cs`, `App.axaml`/`App.axaml.cs`, and
      `src/ClaudeAgentDashboard.Presentation/CompositionRoot.cs` with OS-conditional DI registration stubs
      for the four ports (depends on T019–T022)
- [ ] T024 Implement `TrayIconController` showing a persistent tray/menu-bar icon with a Quit action, in
      `src/ClaudeAgentDashboard.Presentation/TrayIcon/TrayIconController.cs` (depends on T023) — satisfies
      the baseline "always-present icon" requirement (FR-001) all stories build on

**Checkpoint**: Foundation ready — user story implementation can now begin.

---

## Phase 3: User Story 1 - See all running agents at a glance (Priority: P1) 🎯 MVP

**Goal**: Clicking the tray/menu-bar icon opens a window listing every currently running Claude Code
agent, updating as agents start, with an empty state when none are running.

**Independent Test**: Start several Claude Code CLI sessions in different terminal windows, click the
tray/menu-bar icon, and confirm the popover lists one entry per running agent, updating as new agents
start — see quickstart.md scenario 1.

### Tests for User Story 1

> Write these tests FIRST, confirm they FAIL, then implement.

- [ ] T025 [P] [US1] Write failing integration test: `WindowsProcessAgentWatcher.GetCurrentSessions()`
      detects a real spawned process matching the Claude Code CLI signature, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/WindowsProcessAgentWatcherTests.cs`
      (Windows-only)
- [ ] T026 [P] [US1] Write failing integration test: `MacProcessAgentWatcher.GetCurrentSessions()` detects
      a real spawned process matching the Claude Code CLI signature, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/MacProcessAgentWatcherTests.cs`
      (macOS-only)
- [ ] T027 [P] [US1] Write failing unit test: `OpenDashboardQuery` returns every session (already-running
      and newly started) from a faked `IAgentWatcher`, and an empty result when none are running, in
      `tests/ClaudeAgentDashboard.Application.UnitTests/OpenDashboardQueryTests.cs`

### Implementation for User Story 1

- [ ] T028 [US1] Implement `WindowsProcessAgentWatcher` (WMI process enumeration + command-line matching +
      `SessionStarted` on newly detected processes) in
      `src/ClaudeAgentDashboard.Infrastructure/Windows/WindowsProcessAgentWatcher.cs`, making T025 pass
      (depends on T016, T019)
- [ ] T029 [US1] Implement `MacProcessAgentWatcher` (`ps` enumeration + command-line matching +
      `SessionStarted`) in `src/ClaudeAgentDashboard.Infrastructure/MacOS/MacProcessAgentWatcher.cs`,
      making T026 pass (depends on T016, T019)
- [ ] T030 [US1] Implement `OpenDashboardQuery` in
      `src/ClaudeAgentDashboard.Application/UseCases/OpenDashboardQuery.cs`, making T027 pass
- [ ] T031 [US1] Implement `AgentListWindow` (list bound to sessions, running status, empty state) in
      `src/ClaudeAgentDashboard.Presentation/Views/AgentListWindow.axaml` and `.axaml.cs` (depends on T030)
- [ ] T032 [US1] Wire tray icon click → `OpenDashboardQuery` → `AgentListWindow`, and register the
      OS-appropriate `IAgentWatcher` in `CompositionRoot`, in
      `src/ClaudeAgentDashboard.Presentation/TrayIcon/TrayIconController.cs` and
      `src/ClaudeAgentDashboard.Presentation/CompositionRoot.cs` (depends on T028, T029, T031)

**Checkpoint**: User Story 1 is fully functional and independently testable.

---

## Phase 4: User Story 2 - Jump straight to an agent's window (Priority: P1)

**Goal**: Clicking "Show" on a listed agent brings that agent's terminal window to the foreground and
focuses it, informing the user instead of failing silently if that window is gone.

**Independent Test**: With one or more agents running, click "Show" on a specific list entry and confirm
the correct terminal window is raised and focused, including when minimized — see quickstart.md scenario 2.

### Tests for User Story 2

> Write these tests FIRST, confirm they FAIL, then implement.

- [ ] T033 [P] [US2] Write failing integration test: `Win32WindowFocuser` brings a real spawned window to
      the foreground and reports success; reports "not available" once that window is closed, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/Win32WindowFocuserTests.cs` (Windows-only)
- [ ] T034 [P] [US2] Write failing integration test: `MacWindowFocuser` activates a real running
      application and reports success; reports "not available" once it has quit, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/MacWindowFocuserTests.cs` (macOS-only)
- [ ] T035 [P] [US2] Write failing unit test: `ShowAgentCommand` calls `IWindowFocuser.Focus` with the
      session's `TerminalWindowReference` and surfaces the `FocusResult`, using a faked `IWindowFocuser`,
      in `tests/ClaudeAgentDashboard.Application.UnitTests/ShowAgentCommandTests.cs`

### Implementation for User Story 2

- [ ] T036 [US2] Implement `Win32WindowFocuser` (`EnumWindows`/`GetWindowThreadProcessId`/
      `SetForegroundWindow`, with the `AttachThreadInput` foreground-lock workaround) in
      `src/ClaudeAgentDashboard.Infrastructure/Windows/Win32WindowFocuser.cs`, making T033 pass (depends on
      T017, T020)
- [ ] T037 [US2] Implement `MacWindowFocuser` (`NSRunningApplication.activateWithOptions` interop) in
      `src/ClaudeAgentDashboard.Infrastructure/MacOS/MacWindowFocuser.cs`, making T034 pass (depends on
      T017, T020)
- [ ] T038 [US2] Implement `ShowAgentCommand` in
      `src/ClaudeAgentDashboard.Application/UseCases/ShowAgentCommand.cs`, making T035 pass
- [ ] T039 [US2] Add a "Show" button and FR-011 "window no longer available" messaging to `AgentListWindow`
      in `src/ClaudeAgentDashboard.Presentation/Views/AgentListWindow.axaml` and `.axaml.cs` (depends on
      T038; touches the same file as T031 — sequence after it)
- [ ] T040 [US2] Register the OS-appropriate `IWindowFocuser` in `CompositionRoot`, in
      `src/ClaudeAgentDashboard.Presentation/CompositionRoot.cs` (depends on T036, T037)

**Checkpoint**: User Stories 1 AND 2 both work independently.

---

## Phase 5: User Story 3 - Get notified the moment an agent finishes (Priority: P2)

**Goal**: An OS-native notification appears when a running agent finishes; clicking it focuses the
correct window without opening the dashboard first, and the list reflects the finished status until
dismissed.

**Independent Test**: Let an agent run to completion while the dashboard is closed and another
application has focus; confirm a native notification appears and, when clicked, focuses the correct
window — see quickstart.md scenario 3.

### Tests for User Story 3

> Write these tests FIRST, confirm they FAIL, then implement.

- [ ] T041 [P] [US3] Write failing integration test: `WindowsProcessAgentWatcher` raises `SessionFinished`
      within the poll interval after a tracked process exits, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/WindowsProcessAgentWatcherTests.cs`
- [ ] T042 [P] [US3] Write failing integration test: `MacProcessAgentWatcher` raises `SessionFinished`
      within the poll interval after a tracked process exits, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/MacProcessAgentWatcherTests.cs`
- [ ] T043 [P] [US3] Write failing integration test: `WindowsToastNotifier` delivers a real toast for a
      finished session and raises `NotificationActivated` when clicked, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/WindowsToastNotifierTests.cs`
      (Windows-only)
- [ ] T044 [P] [US3] Write failing integration test: `MacUserNotifier` delivers a real
      `UNUserNotification` for a finished session, raises `NotificationActivated` when clicked, and
      reports `WasDelivered = false` when authorization is denied, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/MacUserNotifierTests.cs` (macOS-only)
- [ ] T045 [P] [US3] Write failing unit test: `HandleNotificationActivatedCommand` resolves the correct
      session's `TerminalWindowReference` and calls `IWindowFocuser.Focus` when
      `INotifier.NotificationActivated` fires, using faked `INotifier` and `IWindowFocuser`, in
      `tests/ClaudeAgentDashboard.Application.UnitTests/HandleNotificationActivatedCommandTests.cs`
- [ ] T046 [P] [US3] Write failing unit test: `DismissAgentCommand` removes a `Finished` session from the
      active list and is a no-op for a `Running` session, in
      `tests/ClaudeAgentDashboard.Application.UnitTests/DismissAgentCommandTests.cs`

### Implementation for User Story 3

- [ ] T047 [US3] Extend `WindowsProcessAgentWatcher` with finish-polling and the `SessionFinished` event,
      making T041 pass (depends on T028)
- [ ] T048 [US3] Extend `MacProcessAgentWatcher` with finish-polling and the `SessionFinished` event,
      making T042 pass (depends on T029)
- [ ] T049 [US3] Implement `WindowsToastNotifier` (AppUserModelID registration + toast APIs +
      click-activation callback), making T043 pass, in
      `src/ClaudeAgentDashboard.Infrastructure/Windows/WindowsToastNotifier.cs` (depends on T018, T021)
- [ ] T050 [US3] Implement `MacUserNotifier` (`UNUserNotificationCenter` interop + authorization request +
      click-activation callback), making T044 pass, in
      `src/ClaudeAgentDashboard.Infrastructure/MacOS/MacUserNotifier.cs` (depends on T018, T021)
- [ ] T051 [US3] Implement `HandleNotificationActivatedCommand` in
      `src/ClaudeAgentDashboard.Application/UseCases/HandleNotificationActivatedCommand.cs`, making T045
      pass
- [ ] T052 [US3] Implement `DismissAgentCommand` in
      `src/ClaudeAgentDashboard.Application/UseCases/DismissAgentCommand.cs`, making T046 pass
- [ ] T053 [US3] Update `AgentListWindow` to reflect live `Running → Finished` status changes and add a
      Dismiss action for finished entries, in
      `src/ClaudeAgentDashboard.Presentation/Views/AgentListWindow.axaml` and `.axaml.cs` (depends on
      T051, T052; touches the same file as T031/T039 — sequence after them)
- [ ] T054 [US3] Update `TrayIconController` to indicate unacknowledged finished agents on the
      tray/menu-bar icon (FR-009), in
      `src/ClaudeAgentDashboard.Presentation/TrayIcon/TrayIconController.cs` (depends on T047, T048)
- [ ] T055 [US3] Register the OS-appropriate `INotifier` in `CompositionRoot` and wire
      `NotificationActivated` → `HandleNotificationActivatedCommand`, in
      `src/ClaudeAgentDashboard.Presentation/CompositionRoot.cs` (depends on T049, T050, T051)

**Checkpoint**: All three user stories are independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that support the feature as a whole without belonging to a single user story.

- [ ] T056 [P] Write failing integration test for `JsonSettingsStore` round-tripping
      `LaunchAtLoginEnabled` against a real temp file, in
      `tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/JsonSettingsStoreTests.cs`
- [ ] T057 Implement `JsonSettingsStore` in
      `src/ClaudeAgentDashboard.Infrastructure/Settings/JsonSettingsStore.cs`, making T056 pass (depends
      on T022)
- [ ] T058 [P] Register the app as an OS login item (Windows Run registry key / macOS LaunchAgent), gated
      by `ISettingsStore.LaunchAtLoginEnabled`, wired in
      `src/ClaudeAgentDashboard.Presentation/CompositionRoot.cs` (depends on T057)
- [ ] T059 [P] Run quickstart.md validation end-to-end on Windows and record results in
      `specs/001-agent-tray-dashboard/quickstart.md`
- [ ] T060 [P] Run quickstart.md validation end-to-end on macOS and record results in
      `specs/001-agent-tray-dashboard/quickstart.md`
- [ ] T061 [P] Review the SonarCloud/coverage report from CI and address any new-code issues surfaced
- [ ] T062 [P] Update `README.md` with build, run, and architecture overview instructions referencing
      plan.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001–T010)**: No dependencies — start immediately; most tasks parallel.
- **Foundational (T011–T024)**: Depends on Setup completion — BLOCKS all user stories.
- **User Story 1 (T025–T032)**: Depends on Foundational only.
- **User Story 2 (T033–T040)**: Depends on Foundational only; independently testable from US1, but T039
  edits the same `AgentListWindow` file T031 creates, so sequence after it if worked serially.
- **User Story 3 (T041–T055)**: Depends on Foundational; extends the watchers US1 implements (T028, T029)
  and the `AgentListWindow` file US1/US2 touch — independently testable, but naturally sequenced last.
- **Polish (T056–T062)**: Depends on the desired user stories being complete.

### Within Each User Story

- Tests are written and confirmed failing before implementation (constitution Principle II).
- Domain/Application layers before Infrastructure before Presentation wiring.
- Story checkpoint reached before moving to the next priority.

### Parallel Opportunities

- Setup: T002–T006, T008–T010 in parallel.
- Foundational: T011–T014 in parallel; T015, T017–T022 in parallel; T016 depends on T015; T018 depends on
  T016.
- Per user story, the Windows and macOS integration tests/implementations are different files and can run
  in parallel (e.g., T025 ∥ T026; T036 ∥ T037; T041 ∥ T042 ∥ T043 ∥ T044).
- Different user stories can be staffed in parallel once Foundational is complete, keeping in mind the
  shared-file sequencing note on `AgentListWindow` above.

---

## Parallel Example: User Story 1

```bash
# Launch all US1 tests together:
Task: "Integration test WindowsProcessAgentWatcher detection in tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/WindowsProcessAgentWatcherTests.cs"
Task: "Integration test MacProcessAgentWatcher detection in tests/ClaudeAgentDashboard.Infrastructure.IntegrationTests/MacProcessAgentWatcherTests.cs"
Task: "Unit test OpenDashboardQuery in tests/ClaudeAgentDashboard.Application.UnitTests/OpenDashboardQueryTests.cs"

# Then the two per-OS watcher implementations together:
Task: "Implement WindowsProcessAgentWatcher in src/ClaudeAgentDashboard.Infrastructure/Windows/WindowsProcessAgentWatcher.cs"
Task: "Implement MacProcessAgentWatcher in src/ClaudeAgentDashboard.Infrastructure/MacOS/MacProcessAgentWatcher.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: run quickstart.md scenario 1 independently.
5. Demo: a working "see all running agents" tray app.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. User Story 1 → validate → demo (MVP).
3. User Story 2 → validate → demo (can now jump to any agent's window).
4. User Story 3 → validate → demo (full notify-and-focus loop).
5. Polish → login-item registration, cross-platform validation sign-off, README.

---

## Notes

- [P] tasks touch different files with no unmet dependencies.
- [Story] labels trace each task back to its spec.md user story.
- Tests are mandatory here per the project constitution, not optional — confirm each test fails before
  writing the implementation that makes it pass.
- Commit after each task or logical group.
- Stop at any checkpoint to validate a story independently before continuing.
