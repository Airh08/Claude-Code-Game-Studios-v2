# CCGS Unity v2 — Implementation Roadmap

This roadmap turns the completion checklist into an ordered implementation plan. It is intentionally milestone-driven: each phase should leave the repository in a usable and testable state before the next phase begins.

## Current Baseline — Foundation / MVP

**Status: substantially implemented**

The repository currently has the intended foundation: Game Director, Task Router, specialist agents, Project Brain, skills, Unity-first rules, and CLI/project inspection structures. The README identifies the current project status as Foundation / MVP and the next milestone as executable Unity inspection and validation. fileciteturn134file0L2-L2

Recent work also hardened the regression layer: the checked-in golden fixture was repaired to avoid overlap with real scene files, Brain history serialization was made deterministic, the reopen regression was fixed, and .NET build output is ignored. fileciteturn132file0L3-L11 fileciteturn131file0L3-L12

---

# Phase 1 — Stabilize the Foundation

**Goal:** make the current foundation reliable enough that future features can be built on top of it.

### Tasks

- [x] Isolate Golden Tests from mutable user projects.
- [x] Make scanner fixture deterministic.
- [x] Make Analyze fixture deterministic.
- [x] Make Brain regression use an isolated fixture copy.
- [x] Validate stable issue identity.
- [x] Validate `open → resolved`.
- [x] Validate `resolved → reopened`.
- [x] Persist structured issue history.
- [x] Fix Windows line-ending sensitivity in Brain regression.
- [x] Ignore `bin/` and `obj/` build output.
- [ ] Add CI workflow for the current regression suite.
- [ ] Add explicit supported toolchain versions.
- [ ] Add a clean-checkout verification script.

### Exit criteria

- Scanner, Analyze, and Brain regression suites pass from a clean checkout.
- Tests do not depend on the state of a user's Unity project.
- Brain schema and migrations are deterministic.

---

# Phase 2 — Complete Project Brain

**Goal:** turn Project Brain from issue/state persistence into the durable source of truth for project execution.

### Tasks

- [ ] Finalize Brain schema v3.
- [ ] Add schema migration infrastructure.
- [ ] Add architecture entities/systems.
- [x] Add active task records.
- [x] Add task ownership.
- [ ] Add task execution status.
- [ ] Add validation evidence records.
- [ ] Add ADR records.
- [ ] Add failure-memory records.
- [ ] Link issues ↔ tasks ↔ agents ↔ evidence.
- [ ] Add Brain integrity checker.
- [ ] Add atomic writes/recovery behavior.
- [ ] Add concurrency protection.
- [ ] Add Brain CLI inspection/status command.

### Exit criteria

A fresh analysis can populate Brain, a task can be created and tracked, evidence can be attached to it, and the entire lifecycle survives another CCGS invocation.

---

# Phase 3 — Executable Task Router

**Goal:** transform the current Task Router agent definition into a deterministic routing subsystem.

### Tasks

- [x] Define task schema.
- [ ] Define agent capability schema.
- [x] Define routing rules.
- [x] Map health issue codes to capabilities.
- [x] Map structured task categories to capabilities. (Routes on the controlled `task.type` vocabulary set at creation time, e.g. `ui`/`technical-art`/`testing`; free-text objective/intent classification is not implemented and remains future work.)
- [x] Implement smallest-useful-team selection.
- [x] Support single-agent tasks.
- [x] Support multi-agent tasks.
- [x] Explain routing decisions.
- [x] Persist routing decisions in Brain.
- [x] Detect conflicts/unsupported tasks.
- [x] Add route command to CLI.
- [x] Add deterministic routing regression suite.

### Initial routing matrix

| Task / Issue | Primary Agent | Optional Agent |
|---|---|---|
| Unity configuration / Build Settings | `unity-engineer` | `qa-engineer` |
| Input System / gameplay integration | `unity-engineer` | `gameplay-programmer` |
| Gameplay mechanics | `gameplay-programmer` | `systems-programmer` |
| Architecture/system design | `systems-programmer` | `unity-engineer` |
| UI implementation | `ui-engineer` | `gameplay-programmer` |
| Technical art / asset pipeline | `technical-artist` | `unity-engineer` |
| Game rules/design | `game-designer` | `gameplay-programmer` |
| Testing/regression | `qa-engineer` | `unity-engineer` |

### Exit criteria

Given the same project state and task, the router chooses the same minimal team and records why.

---

# Phase 4 — Agent Execution Contracts

**Goal:** make agents interoperable rather than independent Markdown personas.

### Tasks

- [x] Define common agent input contract.
- [x] Define common agent output contract.
- [ ] Define allowed file scope.
- [x] Define required pre-change inspection. (Already established by `CLAUDE.md` principle 1 and `.claude/rules/architecture.md`; the new Input Contract makes it concrete per field: `brain_context` and `relevant_files` must be inspected before acting.)
- [x] Define evidence requirements.
- [ ] Define handoff format.
- [x] Define failure/escalation format.
- [ ] Define retry behavior.
- [ ] Require agents to update Brain where appropriate.
- [ ] Add representative execution test for every specialist agent.

### Exit criteria

Any specialist can receive a routed task, inspect context, produce a plan/result, identify changed files, report tests, and hand off or complete the task using the same contract.

---

# Phase 5 — End-to-End Orchestration

**Goal:** connect Game Director → Router → Agents → Validation → Brain.

### Target workflow

```text
User request
    ↓
Game Director
    ↓
Analyze / Project Brain
    ↓
Task decomposition
    ↓
Task Router
    ↓
Specialist agent(s)
    ↓
Plan
    ↓
Implement
    ↓
Test
    ↓
Review
    ↓
Fix if needed
    ↓
Update Brain
    ↓
Final evidence/report
```

### Tasks

- [ ] Implement task decomposition.
- [ ] Implement dependency-aware task plans.
- [ ] Implement agent handoffs.
- [ ] Track task state.
- [ ] Prevent conflicting concurrent edits.
- [ ] Collect command/test evidence.
- [ ] Feed failures into `/fix`.
- [ ] Require `/review` before completion for code-changing tasks.
- [ ] Persist final state in Brain.
- [ ] Add end-to-end orchestration test.

### Exit criteria

A single user request can travel through the complete workflow and finish with validated changes and durable project state.

---

# Phase 6 — Unity Editor Integration

**Goal:** move beyond filesystem inspection into real Unity validation.

### Tasks

- [ ] Implement Unity batch-mode launcher.
- [ ] Implement compile validation.
- [ ] Capture Unity Editor logs.
- [ ] Validate scenes through Unity.
- [ ] Validate prefabs through Unity.
- [ ] Detect missing scripts/components.
- [ ] Validate serialized references.
- [ ] Run Edit Mode tests.
- [ ] Run Play Mode smoke tests.
- [ ] Produce machine-readable Unity test results.
- [ ] Attach Unity evidence to Brain/task records.

### Exit criteria

CCGS can make a Unity change and prove through Unity itself that the project compiles and the relevant validation passes.

---

# Phase 7 — Automated Gameplay Validation

**Goal:** enable evidence-based game behavior validation.

### Tasks

- [ ] Define gameplay smoke-test conventions.
- [ ] Add test scene conventions.
- [ ] Add Play Mode automation.
- [ ] Add input simulation where appropriate.
- [ ] Capture failures and logs.
- [ ] Support screenshots/video or other evidence where useful.
- [ ] Associate failures with Brain issues.
- [ ] Add regression fixtures for common gameplay failures.

### Exit criteria

A gameplay agent can implement a mechanic and QA can automatically verify the relevant behavior in Unity.

---

# Phase 8 — Durable Engineering Memory

**Goal:** make CCGS improve its decisions over time without treating stale memory as truth.

### Tasks

- [ ] Implement ADR creation/retrieval.
- [ ] Implement failure-memory creation/retrieval.
- [ ] Link decisions to affected systems.
- [ ] Link failures to fixes and evidence.
- [ ] Define memory freshness rules.
- [ ] Define when memory can influence routing/planning.
- [ ] Add memory regression tests.

### Exit criteria

A later task can discover a relevant architectural decision or previous failure, use it appropriately, and still verify current project state before acting.

---

# Phase 9 — Safety and Reliability

**Goal:** make autonomous modification safe enough for real projects.

### Tasks

- [ ] Add dry-run mode.
- [ ] Add project-root write guard.
- [ ] Add changed-file scope guard.
- [ ] Add backup/recovery for risky operations.
- [ ] Make Brain writes atomic.
- [ ] Validate generated files before replacing originals.
- [ ] Detect partial task execution.
- [ ] Implement safe retry.
- [ ] Add recovery tests.

### Exit criteria

A failed or interrupted operation cannot silently corrupt the project or Project Brain, and the user can understand what happened and recover safely.

---

# Phase 10 — Developer Experience

**Goal:** make CCGS usable by someone who did not build it.

### Tasks

- [ ] Installation/bootstrap flow.
- [ ] Document CLI usage.
- [ ] Document Claude setup.
- [ ] Document agent creation.
- [ ] Document skill creation.
- [ ] Document Task Router rules.
- [ ] Document Brain schema.
- [ ] Add troubleshooting guide.
- [ ] Add example project.
- [ ] Add first-run validation command.
- [ ] Add clear error messages.

### Exit criteria

A new user can clone the repository, configure the required tools, point CCGS at a Unity project, run the first analysis, and understand the resulting project state without prior knowledge of the implementation.

---

# Phase 11 — CI / Release Engineering

**Goal:** prevent regressions and make releases reproducible.

### Tasks

- [ ] GitHub Actions for .NET build/test.
- [ ] Golden scanner regression in CI.
- [ ] Analyze regression in CI.
- [ ] Brain regression in CI.
- [ ] Routing tests in CI.
- [ ] CLI integration tests in CI.
- [ ] Unity validation workflow where runner/licensing permits.
- [ ] Versioning policy.
- [ ] Changelog.
- [ ] Release tags.
- [ ] Release smoke test.

### Exit criteria

A clean commit is automatically validated and a release can be reproduced from a tagged revision.

---

# Phase 12 — Final Product Validation

**Goal:** prove that CCGS is a usable AI-assisted Unity development framework, not just a collection of prompts and utilities.

### Final scenario

1. Clone CCGS from scratch.
2. Configure supported dependencies.
3. Point CCGS at a real Unity project.
4. Run analysis.
5. Populate Project Brain.
6. Submit a non-trivial gameplay/Unity task.
7. Decompose and route it.
8. Execute the minimal specialist team.
9. Modify the Unity project.
10. Run compile and automated tests.
11. Detect a deliberate failure.
12. Use `/fix` to recover.
13. Review the change.
14. Persist an architectural decision or failure memory where appropriate.
15. Re-run analysis.
16. Confirm Brain consistency.
17. Produce final evidence and report.

### Exit criteria

All steps pass on a clean checkout against a supported Unity project and the resulting project is usable.

---

# Milestone Summary

| Milestone | Outcome | Status |
|---|---|---|
| M0 | Repository / agent foundation | 🟢 Foundation exists |
| M1 | Deterministic inspection + regression | 🟢 Implemented |
| M2 | Durable Project Brain | 🟡 Core exists; execution memory remains |
| M3 | Executable Task Router | 🟢 Task model, deterministic rules, `ccgs route`, and Brain-persisted decisions implemented; agent capability schema (M4.3) remains |
| M4 | Agent execution contracts | 🟡 Input/output contract and escalation format defined for all 9 agents; file-scope permissions, handoff format, and per-agent execution tests remain |
| M5 | End-to-end orchestration | 🔴 Pending |
| M6 | Unity Editor validation | 🔴 Pending |
| M7 | Gameplay Play Mode validation | 🔴 Pending |
| M8 | ADR / failure memory | 🔴 Pending |
| M9 | Safety / recovery | 🔴 Pending |
| M10 | Developer experience | 🔴 Pending |
| M11 | CI / release engineering | 🔴 Pending |
| M12 | Final product validation | 🔴 Pending |

## Recommended Next Step

**Do not add more specialist agents yet.** The repository already contains the initial specialist set. fileciteturn135file0L2-L3

The M3 vertical slice is implemented and passes deterministically:

```text
ccgs analyze
    ↓
Project Brain
    ↓
ccgs route (--task <id> or --issue <code>)
    ↓
Task + selected agent(s)
    ↓
structured routing artifact (matched rule, rationale, subject facts)
    ↓
persisted into project-brain/tasks.yaml (routed_agent, routing_rule, rationale, routed_at_utc) when routing a task
```

M3 is complete. M4.1 (input contract) and M4.2 (output contract) are also complete: `.claude/rules/agents.md` defines the shared `task`/`routing`/`brain_context`/`relevant_files` input shape and the `result:` output schema (`status`, `plan`, `changes`, `evidence`, `tests`, `unresolved_risks`, `follow_up_tasks`), and all 9 agent definitions under `.claude/agents/` document their role-specific contract against it, checked by `tools/tests/run-agent-contracts-test.ps1`.

The next implementation should be **M4.3: Agent capability metadata**, because `TaskRouter` (`tools/ccgs-cli/TaskRouter.cs`) still resolves agents from a hard-coded switch statement rather than reading machine-readable capability metadata (domains, tools, read/write scope, supported task types, required validators) from the agent definitions themselves. After that, M4.4 (per-agent validation requirements) and then M5 (orchestration) build on top — still no need to expand the agent count first.
