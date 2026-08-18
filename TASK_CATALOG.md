# CCGS v2 — Task Catalog

This document expands the tasks referenced by `PROJECT_COMPLETION_CHECKLIST.md` and `ROADMAP.md` into actionable implementation units. It is the operational companion to those documents.

## How to use this document

- `[ ]` = not complete.
- `[~]` = partially implemented.
- `[x]` = implemented and validated.
- A task is only considered complete when its implementation exists, documentation is updated, and the relevant regression/integration test passes.
- Dependencies describe the minimum prerequisite work; they do not imply that every task must be implemented in a single commit.

---

# M0 — Foundation

## M0.1 Repository structure
- [x] Define the top-level layout for `.claude`, `tools`, tests, documentation, and project integration.
- **Description:** Keep agent definitions, skills, deterministic tooling, fixtures, and documentation separated so each subsystem can evolve independently.
- **Done when:** A new contributor can identify where agents, skills, CLI code, tests, fixtures, and project-facing artifacts belong.

## M0.2 Agent conventions
- [x] Define a common format for agent prompts.
- **Description:** Every agent should expose its role, responsibilities, constraints, expected inputs, outputs, and validation expectations.
- **Done when:** Agents can be reviewed and invoked consistently without relying on undocumented conventions.

## M0.3 Git and safety conventions
- [x] Establish Git, Unity, architecture, and testing rules.
- **Description:** Prevent agents from making unsafe repository or Unity changes and establish a consistent development workflow.
- **Done when:** Agent behavior is constrained by repository-level rules and destructive operations require explicit policy handling.

---

# M1 — Deterministic Project Inspection

## M1.1 Project scanner
- [x] Inspect Unity version, project validity, Input System, packages, scripts, scenes, prefabs, assemblies, and test directories.
- **Description:** Produce deterministic filesystem/project metadata without requiring an LLM.
- **Done when:** Identical project state produces identical scanner facts.

## M1.2 Build Settings validation
- [x] Detect missing scene paths and GUID mismatches.
- **Description:** Compare Build Settings references with actual scene and `.meta` files.
- **Done when:** Missing references are emitted as stable `BUILD-001` issues.

## M1.3 Input callback validation
- [x] Detect potentially unsafe `InputAction.CallbackContext` usage with SendMessages.
- **Description:** Flag patterns that require Unity Editor verification instead of pretending static inspection proves runtime behavior.
- **Done when:** `INPUT-001` is deterministic and clearly describes the limitation.

## M1.4 Deterministic golden fixture
- [x] Maintain a controlled Unity-like fixture independent of the user's project.
- **Description:** Regression tests must not depend on the current state of `Project_ecp` or another evolving game.
- **Done when:** Golden tests pass regardless of changes made to the user's real project.

---

# M2 — Project Brain

## M2.1 Brain artifacts
- [x] Generate `project.yaml`, `state.yaml`, `architecture.yaml`, and `issues.yaml`.
- **Description:** Persist project knowledge outside transient CLI output so later agents can reason from the same state.
- **Done when:** `--sync-brain` creates a valid and deterministic Brain.

## M2.2 Stable issue identity
- [x] Generate stable IDs from issue identity.
- **Description:** The same logical problem must retain its ID between analyses.
- **Done when:** Consecutive analyses do not duplicate unchanged issues.

## M2.3 Issue lifecycle
- [x] Support `open`, `resolved`, and `reopened` transitions.
- **Description:** Reconciliation compares the latest health report with persisted issues and records lifecycle changes.
- **Done when:** A regression test demonstrates `open → resolved → reopened → resolved` with stable IDs.

## M2.4 Structured history
- [x] Store issue history as structured YAML rather than embedded JSON strings.
- **Description:** Agents should be able to inspect lifecycle history without parsing JSON embedded inside YAML.
- **Done when:** Legacy history can be migrated without data loss and new history is structured.

## M2.5 Brain schema versioning
- [ ] Define explicit migration rules for every future Brain schema version.
- **Description:** Changes to persisted artifacts must be backward-compatible or provide deterministic migrations.
- **Dependencies:** M2.4.
- **Done when:** A fixture from each supported previous schema can be upgraded automatically.

## M2.6 Brain query API
- [ ] Add commands/functions to query open issues, resolved issues, architecture facts, and recent history.
- **Description:** Agents should not need to parse YAML manually for common queries.
- **Dependencies:** M2.1–M2.4.
- **Done when:** CLI and/or library APIs expose stable query operations with tests.

---

# M3 — Task Router

## M3.1 Task model
- [x] Define a canonical task schema.
- **Description:** Represent task ID, objective, type, priority, affected paths, constraints, dependencies, assigned agent, status, and validation requirements. Implemented as `BrainTask`/`TaskStore` (`tools/ccgs-cli/TaskStore.cs`), persisted to `project-brain/tasks.yaml`, exposed via `ccgs task create`/`ccgs task list`.
- **Dependencies:** M2.1, M2.6.
- **Done when:** Tasks can be serialized/deserialized deterministically. Covered by `tools/ccgs-cli/tests/run-task-model-test.ps1` (multi-item and empty list fields, unique IDs, LF-only output, repeated-read determinism).

## M3.2 Routing rules
- [x] Define routing rules mapping task characteristics to agents.
- **Description:** Use deterministic rules before LLM reasoning where possible. Implemented as `TaskRouter` (`tools/ccgs-cli/TaskRouter.cs`): `RouteIssueCode` maps known health issue codes (`BUILD-001`, `INPUT-001`, `TEST-001`, `PROJECT-001`) to primary/supporting agents per the ROADMAP routing matrix; `RouteTask` maps task `type` (`ui`, `technical-art`, `testing`, `design`, `content`, `analysis`) to agents, honoring an explicit `--agent` assignment first. Unmatched codes/types deliberately return no agent rather than guessing.
- **Dependencies:** M3.1.
- **Done when:** Representative tasks route to predictable primary/supporting agents. Covered by `tools/ccgs-cli/tests/run-routing-rules-test.ps1`.

## M3.3 Router CLI
- [x] Implement `ccgs route`.
- **Description:** Read a task or Brain issue, resolve the appropriate agent(s), and emit a routing artifact. `ccgs route <project-root> --task <id>` reads `project-brain/tasks.yaml`; `ccgs route <project-root> --issue <code>` routes a bare issue code. Routing decisions are not yet persisted back into Project Brain (open follow-up).
- **Dependencies:** M3.1–M3.2.
- **Done when:** A task can be routed without manually inspecting agent files.

## M3.4 Routing explanation
- [x] Record why an agent was selected.
- **Description:** Routing should be observable and auditable rather than a black box. The routing artifact includes `MatchedRule`, `Rationale`, and `SubjectFacts` (the task's objective/type/priority, or the issue code) alongside `PrimaryAgent`/`SupportingAgents`.
- **Dependencies:** M3.3.
- **Done when:** Output contains matched rules, relevant project facts, and selected agents.

## M3.5 Router regression suite
- [x] Add deterministic routing tests.
- **Description:** Prevent changes to agent definitions or routing rules from silently changing assignments. `tools/ccgs-cli/tests/run-routing-rules-test.ps1` covers every known issue code, every mapped task type, explicit-agent override, unmatched fallback (both issue and task), and the missing-task error path. CI wiring for the full regression suite is still open (Phase 1 / M11.1).
- **Dependencies:** M3.3.
- **Done when:** Golden routing cases pass in CI.

---

# M4 — Agent Contracts

## M4.1 Agent input contract
- [ ] Define the information every agent receives.
- **Description:** Include task, project context, Brain facts, constraints, relevant files, and validation requirements.
- **Dependencies:** M3.1.
- **Done when:** Every production agent has a documented input contract.

## M4.2 Agent output contract
- [ ] Define structured agent results.
- **Description:** Results should distinguish plan, changes, evidence, tests, unresolved risks, and follow-up tasks.
- **Dependencies:** M4.1.
- **Done when:** Results can be consumed by another agent without relying on prose conventions.

## M4.3 Agent capability metadata
- [ ] Add machine-readable capabilities to agents.
- **Description:** Describe domains, tools, read/write scope, supported task types, and required validators.
- **Dependencies:** M4.1–M4.2.
- **Done when:** Task Router can select agents from metadata rather than hard-coded names alone.

## M4.4 Agent validation requirements
- [ ] Define per-agent completion criteria.
- **Description:** For example, gameplay changes may require compile checks, Unity scene validation, and targeted tests.
- **Dependencies:** M4.2.
- **Done when:** An agent cannot report success without producing required evidence.

---

# M5 — Orchestration

## M5.1 Execution engine
- [ ] Implement a controlled mechanism for invoking routed agents.
- **Description:** Convert a routing decision into an actual agent execution while preserving context and artifacts.
- **Dependencies:** M3.3, M4.1–M4.4.
- **Done when:** A task can move from routed to executing to completed/failed state.

## M5.2 Multi-agent coordination
- [ ] Support primary and supporting agents.
- **Description:** Allow a gameplay task to involve, for example, `gameplay-programmer` plus `unity-engineer` without losing ownership.
- **Dependencies:** M5.1.
- **Done when:** Dependencies and handoffs are represented explicitly.

## M5.3 Execution state
- [ ] Persist task execution state.
- **Description:** Record attempts, agent assignments, timestamps, artifacts, tests, failures, and recovery actions.
- **Dependencies:** M2.6, M5.1.
- **Done when:** Interrupted work can be resumed without reconstructing context manually.

## M5.4 Failure and retry handling
- [ ] Implement bounded retries and escalation.
- **Description:** Prevent infinite agent loops and route failures to an appropriate fallback or human decision.
- **Dependencies:** M5.3.
- **Done when:** Failed tasks become explicit recoverable states rather than silent failures.

---

# M6 — Unity Integration

## M6.1 Unity project bridge
- [ ] Define the supported mechanism for communicating with the Unity project/editor.
- **Description:** Establish how CCGS requests editor actions and receives validation results.
- **Dependencies:** M5.1.
- **Done when:** CCGS can distinguish filesystem-only operations from Editor-dependent operations.

## M6.2 Unity validation commands
- [ ] Add deterministic validation for scenes, assets, scripts, prefabs, and Build Settings where applicable.
- **Description:** Complement static scanning with actual Unity-side validation.
- **Dependencies:** M6.1.
- **Done when:** Representative project changes can be validated through Unity.

## M6.3 Safe editor actions
- [ ] Define allowlisted Unity mutations.
- **Description:** Agents should only perform supported, reversible editor operations.
- **Dependencies:** M6.1–M6.2, M9.1.
- **Done when:** Destructive or ambiguous actions are blocked or require explicit approval.

---

# M7 — Gameplay Validation

## M7.1 Automated Unity tests
- [ ] Establish EditMode and PlayMode test conventions.
- **Description:** Give gameplay agents a reliable feedback loop.
- **Dependencies:** M6.2.
- **Done when:** A baseline test suite executes deterministically.

## M7.2 Gameplay regression fixtures
- [ ] Create representative gameplay scenarios.
- **Description:** Test movement, input, interactions, state changes, scenes, and other supported mechanics.
- **Dependencies:** M7.1.
- **Done when:** Common agent changes have automated regression coverage.

## M7.3 Test result ingestion
- [ ] Feed Unity test results back into Brain/task execution state.
- **Description:** Test failures become actionable issues rather than detached console output.
- **Dependencies:** M2.6, M5.3, M7.1.
- **Done when:** A failed test can automatically produce or update a Brain issue.

---

# M8 — Durable Memory

## M8.1 Architecture decisions
- [ ] Persist Architecture Decision Records.
- **Description:** Record important technical decisions, alternatives, rationale, consequences, and affected systems.
- **Dependencies:** M2.1.
- **Done when:** Agents can retrieve historical design decisions.

## M8.2 Task history
- [ ] Persist completed and failed task records.
- **Description:** Preserve what was attempted, what changed, what failed, and what validation succeeded.
- **Dependencies:** M5.3.
- **Done when:** Future agents can learn from prior task execution.

## M8.3 Memory retrieval
- [ ] Provide relevance-oriented retrieval over Brain artifacts.
- **Description:** Surface only the project knowledge relevant to the current task.
- **Dependencies:** M8.1–M8.2.
- **Done when:** Agents receive useful historical context without loading the entire project state.

---

# M9 — Safety and Recovery

## M9.1 Permission model
- [ ] Define read-only, safe-write, and destructive capabilities.
- **Description:** Agent authority must be explicit and enforceable.
- **Dependencies:** M4.3, M5.1.
- **Done when:** An agent cannot perform operations outside its declared scope.

## M9.2 Change preview
- [ ] Provide a reviewable representation of proposed changes before risky execution.
- **Description:** Show affected files, intended Unity actions, and expected validation.
- **Dependencies:** M5.1, M9.1.
- **Done when:** Risky changes can be approved or rejected before execution.

## M9.3 Rollback/recovery
- [ ] Define recovery from failed agent operations.
- **Description:** Use Git, backups, or transactional editor mechanisms where appropriate.
- **Dependencies:** M9.1–M9.2.
- **Done when:** A failed task can restore a known-good state.

## M9.4 Human escalation
- [ ] Define conditions requiring human approval.
- **Description:** Ambiguous requirements, destructive operations, repeated failures, and unsafe migrations should stop automation.
- **Dependencies:** M5.4, M9.1.
- **Done when:** Escalation is explicit, observable, and resumable.

---

# M10 — Developer Experience

## M10.1 Unified CLI
- [ ] Provide coherent commands for scan, analyze, route, plan, implement, test, review, and status.
- **Description:** Hide internal implementation details behind a stable user-facing CLI.
- **Dependencies:** M3–M7.
- **Done when:** A developer can operate the full workflow from the CLI.

## M10.2 Human-readable output
- [ ] Add concise terminal summaries and machine-readable output modes.
- **Description:** Humans need useful summaries while automation needs structured artifacts.
- **Dependencies:** M10.1.
- **Done when:** Every major command supports both consumption styles where appropriate.

## M10.3 Diagnostics
- [ ] Add structured logs and execution diagnostics.
- **Description:** Make failures explainable without reading implementation code.
- **Dependencies:** M5.3, M10.1.
- **Done when:** An execution can be traced from task creation through validation.

## M10.4 Documentation
- [ ] Keep README, architecture docs, task catalog, completion checklist, and roadmap synchronized.
- **Description:** Documentation is part of the product, not a final cleanup step.
- **Dependencies:** All milestones.
- **Done when:** Every public workflow has an executable example.

---

# M11 — CI and Release

## M11.1 Continuous integration
- [ ] Run build, unit tests, golden tests, Brain tests, and routing tests in CI.
- **Description:** Prevent regressions from entering the main branch.
- **Dependencies:** M1–M5, M7.
- **Done when:** Required checks run automatically on pull requests and main.

## M11.2 Cross-platform validation
- [ ] Validate supported Windows/Linux/macOS behavior where claimed.
- **Description:** Pay particular attention to paths, PowerShell, line endings, and process invocation.
- **Dependencies:** M11.1.
- **Done when:** Supported environments pass the same acceptance suite.

## M11.3 Release packaging
- [ ] Define how users install and invoke CCGS.
- **Description:** Package CLI, agent definitions, skills, rules, and required runtime assets.
- **Dependencies:** M10.1, M11.1.
- **Done when:** A clean machine can install and run a documented smoke test.

## M11.4 Versioning and changelog
- [ ] Establish semantic versioning and release notes.
- **Description:** Persist schema, CLI, and agent compatibility information across releases.
- **Dependencies:** M2.5, M11.3.
- **Done when:** A release clearly states breaking changes and migration requirements.

---

# M12 — Final Validation

## M12.1 End-to-end happy path
- [ ] Validate `task → route → plan → implement → test → review → complete`.
- **Description:** Prove that CCGS works as an integrated development workflow rather than a collection of independent tools.
- **Dependencies:** M3–M11.
- **Done when:** A representative Unity feature can complete the entire workflow.

## M12.2 End-to-end failure path
- [ ] Validate failure, retry, escalation, and recovery.
- **Description:** The system must behave predictably when agents or Unity validation fail.
- **Dependencies:** M5.4, M9.1–M9.4.
- **Done when:** A deliberately failing task is recovered without corrupting project state.

## M12.3 Memory continuity test
- [ ] Validate that a later task can use previous Brain and ADR information.
- **Description:** Prove that CCGS accumulates useful project knowledge over multiple tasks.
- **Dependencies:** M8.1–M8.3.
- **Done when:** A second task can correctly use context generated by the first task.

## M12.4 Clean-install acceptance test
- [ ] Run the complete workflow from a clean checkout.
- **Description:** Detect hidden dependencies on developer machines, local paths, generated files, or undocumented configuration.
- **Dependencies:** M11.3.
- **Done when:** A clean environment passes the complete acceptance suite.

## M12.5 Completion gate
- [ ] Mark CCGS complete only when all mandatory checklist items are satisfied.
- **Description:** The completion checklist is the final authority; roadmap progress alone does not constitute completion.
- **Dependencies:** M12.1–M12.4.
- **Done when:** `PROJECT_COMPLETION_CHECKLIST.md` contains no unresolved mandatory items and the final acceptance suite passes.

---

# Cross-cutting requirements

These apply to every milestone and should not be treated as optional cleanup:

- [ ] Deterministic behavior where the task does not require LLM judgment.
- [ ] Stable identifiers for persistent entities.
- [ ] Explicit schemas for machine-consumed artifacts.
- [ ] Tests for every new deterministic behavior.
- [ ] Clear distinction between static inspection and Unity runtime/editor validation.
- [ ] Observable execution state and actionable errors.
- [ ] Safe boundaries around filesystem, Git, Unity, and external tool operations.
- [ ] Documentation updated with user-facing behavior changes.

# Priority rule

When choosing the next implementation task, prefer work that unlocks multiple downstream milestones. The current highest-value sequence is:

1. M3 — Task model and executable Task Router.
2. M4 — Agent contracts and capability metadata.
3. M5 — Controlled orchestration.
4. M7 — Unity test/validation feedback loop.
5. M8/M9 — Durable memory and safety/recovery.
6. M10/M11 — Production developer experience and release infrastructure.
7. M12 — End-to-end acceptance.
