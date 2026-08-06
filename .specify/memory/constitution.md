# ClaudeAgentDashboard Constitution

## Core Principles

### I. Clean Architecture Layering
The codebase is organized into four layers with a strict inward dependency
direction: **Domain** (entities, value objects, and port interfaces — zero
outward dependencies on frameworks or infrastructure) → **Application**
(use cases/business logic — depends only on Domain abstractions) →
**Infrastructure** (implements Domain-defined ports: process/window
enumeration, OS notification APIs, persistence — depends on Domain and
Application, never the reverse) → **Presentation** (Avalonia UI, tray icon,
composition root — wires concrete Infrastructure to abstractions via DI).
No Infrastructure concern (a specific OS API, a specific notification
toolkit, a specific windowing library) may leak into Domain or Application
code; if a module needs an OS capability, it depends on an interface owned
by Domain/Application, and Infrastructure implements it.

### II. Test-First (NON-NEGOTIABLE)
Tests are written before the implementation they cover. A new test MUST be
confirmed failing before the code that makes it pass is written.
Red-Green-Refactor is followed strictly — no implementation commits without
a preceding failing test that justifies them.

### III. Three-Layer Test Coverage
Every non-trivial module ships with all three test layers, not unit tests
alone:
- **Unit tests**: individual classes/functions in isolation, with external
  dependencies (OS APIs, process enumeration, notification services) faked
  or mocked behind Domain-owned interfaces.
- **Integration tests**: components working together against realistic
  fakes or the real OS-level surface where practical (e.g., a real
  temporary process/window to detect, a real notification round-trip on
  the target OS) — never mocking the exact thing under test.
- **Architecture tests**: automated enforcement of the layering rule in
  Principle I via `NetArchTest.Rules`, asserting Domain has no outward
  dependencies, Application depends only on Domain abstractions, and
  Infrastructure implementations are referenced only from the composition
  root/Presentation layer. These are CI-enforced tests, not code-review
  conventions.

### IV. SOLID Design
Every module is held to SOLID deliberately:
- **S**ingle Responsibility per class (e.g., agent detection, window
  focusing, and notification dispatch are separate components, not one
  "AgentManager" god class).
- **O**pen/Closed — new agent-detection strategies or notification
  backends are added as new implementations of an existing abstraction,
  not by branching on OS/type inside existing code.
- **L**iskov Substitution — any implementation of a port (e.g., a
  platform-specific window-focuser) must be usable anywhere that port is
  expected, with no surprising platform-specific side effects.
- **I**nterface Segregation — narrow, purpose-specific interfaces (e.g.,
  `IAgentWatcher`, `IWindowFocuser`, `INotifier`) rather than one broad
  interface implementers must partially stub.
- **D**ependency Inversion — Application logic depends on Domain-owned
  interfaces; concrete platform implementations (Win32 interop, AppKit/
  UserNotifications interop) are supplied via dependency injection at the
  composition root.

### V. Code Quality Gate
Static analysis is wired in from the start, not bolted on after the fact:
`SonarAnalyzer.CSharp` runs as a build-time analyzer via
`Directory.Build.props`, and new-code warnings are treated as errors
(`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`) so complexity and
duplication issues are caught before merge. A hosted SonarCloud scan with
coverage (`coverlet.collector`) runs in CI ahead of self-hosting
SonarQube, unless existing infrastructure later makes self-hosting free.

## Technology Constraints

- **Language/Runtime**: C# on .NET (current LTS), no other languages in
  the product codebase.
- **UI Framework**: Avalonia UI, chosen specifically for its single
  codebase cross-platform tray/window support across Windows and macOS
  (Linux is architecturally unblocked but not a launch requirement).
- **Platform-specific interop** (Win32 P/Invoke for window focusing,
  Windows Toast notifications, AppKit/UserNotifications interop on macOS)
  MUST live behind Domain-defined interfaces in the Infrastructure layer —
  never called directly from Application or Presentation code.
- No feature flags or backwards-compatibility shims for pre-release code;
  the codebase has no external consumers yet, so breaking changes are
  made directly rather than shimmed.

## Development Workflow

- A Setup phase precedes feature work on any new project increment:
  analyzer/CI wiring (SonarCloud) and architecture-test scaffolding land
  before the first use case is implemented.
- A Foundational phase defines the Domain layer's entities and port
  interfaces for a feature before any Application/Infrastructure
  implementation begins.
- Per-module task breakdowns split unit, integration, and architecture
  test tasks explicitly — they are never lumped into a single generic
  "tests" task.
- Every feature plan includes an explicit architecture-test task asserting
  the dependency-direction rules in Principle I for that feature's actual
  layering.

## Governance

This constitution supersedes ad hoc practice for this project. Amendments
require a documented rationale and a version bump per semantic versioning
(MAJOR for principle removals/redefinitions, MINOR for new principles or
materially expanded guidance, PATCH for clarifications/wording). All plans
and reviews must verify compliance with Principles I–V; any deviation must
be justified in that feature's plan under Complexity Tracking rather than
silently absorbed.

**Version**: 1.0.0 | **Ratified**: 2026-08-06 | **Last Amended**: 2026-08-06
