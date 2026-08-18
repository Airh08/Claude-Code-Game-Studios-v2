# CCGS Unity v2 — Project Completion Checklist

This document defines what must exist and what must be validated before **CCGS Unity v2** can be considered complete.

The goal is not simply to have files in the repository. Every major capability must be executable, observable, testable, and documented.

## Definition of Done

CCGS v2 is complete when:

- A user can point CCGS at a real Unity 6.x project.
- CCGS can inspect the project deterministically without opening Unity for basic analysis.
- Project health, architecture, state, and issues are persisted in Project Brain.
- Issues retain stable identity across repeated analyses and support open/resolved/reopened lifecycle.
- A task can be routed to the smallest useful specialist team.
- Agents can read project context before modifying anything.
- Agents can plan, implement, test, review, and fix work through a consistent workflow.
- Unity-specific changes can be validated with real evidence rather than only textual reasoning.
- Automated regression tests protect the framework itself.
- Unity integration/play-mode validation exists for supported scenarios.
- Architectural decisions and important failures are persisted as durable memory.
- The system can recover from failures without corrupting Project Brain state.
- Documentation explains installation, usage, architecture, extension points, and limitations.

---

## 1. Repository Foundation

- [x] Repository structure exists and is organized around `.claude`, CLI tooling, tests, and documentation.
- [x] `.gitignore` prevents .NET build artifacts and common local files from polluting the repository. citeturn131file0L7-L12
- [ ] Add a clear versioning/release strategy.
- [ ] Add CI for pull requests and main branch.
- [ ] Add contribution/development guidelines.
- [ ] Define supported Unity and .NET versions explicitly.

## 2. Claude Agent Layer

Current repository structure already contains Game Director, Task Router, gameplay, systems, Unity, QA, UI, technical-art, and design agent definitions. citeturn135file0L2-L3

- [x] Game Director agent.
- [x] Task Router agent definition.
- [x] Gameplay Programmer agent.
- [x] Systems Programmer agent.
- [x] Unity Engineer agent.
- [x] QA Engineer agent.
- [x] UI Engineer agent.
- [x] Technical Artist agent.
- [x] Game Designer agent.
- [ ] Define explicit input/output contracts for every agent.
- [ ] Define when an agent may modify files.
- [ ] Define when an agent must ask for clarification.
- [ ] Add structured task/result artifacts.
- [ ] Add agent handoff protocol.
- [ ] Add failure/escalation protocol.
- [ ] Add agent execution evidence requirements.
- [ ] Validate every agent against at least one representative task.

## 3. Skills / Workflow

The intended workflow is already documented as analyze → route → plan → implement → test → review → fix. fileciteturn134file0L2-L2

- [x] Analyze skill.
- [x] Plan skill.
- [x] Implement skill.
- [x] Test skill.
- [x] Review skill.
- [x] Fix skill.
- [x] Project-health skill.
- [ ] Make skills executable rather than primarily instructional.
- [ ] Define standard artifacts for each skill.
- [ ] Persist task lifecycle in Project Brain.
- [ ] Ensure `/fix` can consume evidence from `/test` and `/review`.
- [ ] Add end-to-end workflow regression test.

## 4. Project Scanner

- [x] Detect Unity project root.
- [x] Detect Unity version.
- [x] Detect Input System.
- [x] Discover scripts, scenes, prefabs, assemblies, and test directories.
- [x] Detect invalid Build Settings scene references.
- [x] Detect InputAction callback warnings.
- [x] Produce deterministic scan output.
- [x] Golden fixture exists for regression testing.
- [x] Golden tests are isolated from mutable user projects.
- [ ] Expand validation to prefabs and serialized component references.
- [ ] Detect missing script/component references.
- [ ] Detect duplicate/conflicting assets where practical.
- [ ] Detect common package/configuration problems.
- [ ] Detect scene dependency relationships.
- [ ] Produce machine-readable diagnostics with stable issue codes.

## 5. CCGS CLI

- [x] `scan` command.
- [x] `analyze` command.
- [x] `--pretty` output.
- [x] `--sync-brain` integration.
- [x] Project-root validation.
- [x] Add `route` command.
- [x] Add `plan` command or task artifact generation.
- [ ] Add `validate` command.
- [ ] Add `status`/Brain inspection command.
- [ ] Add machine-readable exit codes for CI.
- [ ] Add consistent error contract.
- [ ] Add install/bootstrap command or documented global invocation.
- [ ] Add CLI integration tests for all public commands.

## 6. Project Brain

- [x] Project metadata persistence.
- [x] Project state persistence.
- [x] Issue persistence.
- [x] Stable issue IDs.
- [x] `open → resolved` lifecycle.
- [x] `resolved → reopened` lifecycle regression coverage.
- [x] Structured YAML history.
- [x] History migration from legacy embedded JSON.
- [x] Repeated sync persistence regression test.
- [ ] Persist architecture/system entities beyond the initial scaffold.
- [x] Persist active tasks and ownership.
- [ ] Persist task execution status.
- [ ] Persist validation evidence.
- [ ] Persist ADRs.
- [ ] Persist failure memory.
- [ ] Add schema/version migration framework.
- [ ] Add Brain integrity validation and recovery behavior.
- [ ] Define concurrency/locking behavior for multiple agents.

## 7. Task Router

- [x] Implement executable Task Router.
- [x] Define routing rules by issue code/category.
- [x] Define routing rules by task intent.
- [x] Select the smallest useful specialist team.
- [x] Support single-agent and multi-agent tasks.
- [x] Explain why an agent/team was selected.
- [ ] Persist routing decisions in Project Brain.
- [x] Detect unsupported tasks and escalate safely.
- [x] Add deterministic routing tests.

## 8. Game Director / Orchestration

- [ ] Implement end-to-end orchestration around the Game Director.
- [ ] Decompose high-level requests into atomic tasks.
- [ ] Create dependency-aware task plans.
- [ ] Assign tasks through Task Router.
- [ ] Coordinate agent handoffs.
- [ ] Track progress and failures.
- [ ] Prevent conflicting agents from editing the same files simultaneously.
- [ ] Require validation before declaring a task complete.
- [ ] Produce a final execution summary.

## 9. Agent Execution

- [ ] Establish a standard task contract.
- [ ] Establish a standard completion contract.
- [ ] Require agents to inspect relevant Brain state first.
- [ ] Require agents to inspect affected Unity assets before modification.
- [ ] Require minimal-scope changes.
- [ ] Capture changed files.
- [ ] Capture commands/tests executed.
- [ ] Capture validation evidence.
- [ ] Record failures and recovery actions.
- [ ] Support safe retry.

## 10. Unity Integration

- [x] Basic filesystem-based Unity inspection.
- [x] Unity 6.x project detection.
- [ ] Add Unity Editor batch-mode integration.
- [ ] Add compile-error detection from Unity logs.
- [ ] Add scene validation in Unity.
- [ ] Add prefab validation in Unity.
- [ ] Add asset import/serialization validation where needed.
- [ ] Add Play Mode smoke tests.
- [ ] Add automated gameplay regression tests.
- [ ] Add build validation.
- [ ] Capture Unity logs and associate them with tasks/issues.
- [ ] Support evidence artifacts such as logs, screenshots, and test reports.

## 11. Automated Testing

- [x] Scanner golden regression.
- [x] Analyze golden regression.
- [x] Brain persistence regression.
- [x] Brain reopen regression.
- [ ] Unit tests for scanner rules.
- [ ] Unit tests for Brain synchronization.
- [ ] Unit tests for routing.
- [ ] CLI integration tests.
- [ ] Agent workflow tests.
- [ ] Unity Edit Mode test integration.
- [ ] Unity Play Mode test integration.
- [ ] Full end-to-end project test.
- [ ] CI gate requiring relevant tests to pass.

## 12. Memory / Architecture Knowledge

- [ ] ADR storage and retrieval.
- [ ] Failure-memory storage and retrieval.
- [ ] Architecture graph/model.
- [ ] Link issues to architecture entities.
- [ ] Link tasks to decisions and failures.
- [ ] Prevent stale memory from silently overriding current project state.
- [ ] Add explicit memory update rules.

## 13. Safety / Reliability

- [ ] Dry-run mode for destructive or broad operations.
- [ ] File-change scope guardrails.
- [ ] Backup/recovery strategy before risky Unity changes.
- [ ] Prevent edits outside the declared project root.
- [ ] Validate generated files before writing them.
- [ ] Atomic Brain writes.
- [ ] Corruption recovery for Brain files.
- [ ] Clear failure states and actionable diagnostics.

## 14. Documentation

- [x] README with architecture goals and workflow.
- [ ] Installation guide.
- [ ] First-project setup guide.
- [ ] CLI reference.
- [ ] Agent authoring guide.
- [ ] Skill authoring guide.
- [ ] Project Brain schema reference.
- [ ] Task Router rule reference.
- [ ] Unity integration guide.
- [ ] Troubleshooting guide.
- [ ] Architecture decision records for major design choices.
- [ ] Example end-to-end project walkthrough.

## 15. Release Readiness

- [ ] All public CLI commands documented.
- [ ] CI green on a clean checkout.
- [ ] Regression suite green on Windows.
- [ ] Unity integration suite green on supported Unity version(s).
- [ ] No known critical data-loss issues.
- [ ] Brain migration tested across supported schema versions.
- [ ] Example Unity project successfully analyzed, modified, tested, and reviewed end-to-end.
- [ ] Fresh-user setup verified from zero.
- [ ] Release notes/changelog created.
- [ ] Version tagged.

---

## Final Acceptance Test

A release candidate should pass this scenario without manual intervention beyond approving the requested operation:

1. Point CCGS at a real Unity project.
2. Run `/analyze`.
3. Populate/update Project Brain.
4. Submit a gameplay or Unity-engineering task.
5. Route it to the smallest useful specialist team.
6. Generate a plan.
7. Implement the change.
8. Run automated validation.
9. Detect and fix any failure.
10. Review the resulting changes.
11. Persist architecture/failure knowledge when applicable.
12. Produce a final report containing changes, tests, evidence, and remaining issues.
13. Re-run analysis and confirm Brain consistency.

**Only after this scenario passes should CCGS v2 be considered complete.**
