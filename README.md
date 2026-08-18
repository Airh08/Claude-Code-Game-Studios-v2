# CCGS Unity v2

AI-assisted game development framework for Unity 6.x, designed around project awareness, minimal specialist teams, durable project memory, and evidence-based validation.

## What is included

- **Game Director** for orchestration and task decomposition.
- **Task Router** for selecting the smallest useful specialist team.
- **Specialist agents** for design, gameplay, Unity architecture, systems, UI, technical art, and QA.
- **Project Brain** as the source of truth for project architecture and state.
- **Skills** for analyze, plan, implement, test, review, fix, and project health.
- **ADR and failure memory** for durable architectural and debugging knowledge.
- **Unity-first rules**, including Unity Input System enforcement.

## Initial workflow

```text
/analyze
   -> task-router
   -> /plan
   -> /implement
   -> /test
   -> /review
   -> /fix (when needed)
```

## Design goals

1. Understand before modifying.
2. Use the smallest useful agent team.
3. Keep project knowledge synchronized with implementation.
4. Validate changes with real evidence.
5. Make architectural decisions explicit.
6. Build toward automated Unity playtesting and regression validation.

## Current status

Foundation and deterministic inspection are implemented and regression-tested: the project scanner, `ccgs analyze`, and Project Brain issue persistence (stable IDs, `open -> resolved -> reopened` lifecycle, structured history) all pass against a checked-in golden fixture.

The Task Router is now executable: `ccgs task create`/`ccgs task list` persist a canonical task schema to `project-brain/tasks.yaml`, and `ccgs route --task <id>` / `--issue <code>` resolves primary/supporting agents through deterministic rules (not an LLM), recording the matched rule and rationale.

See `ROADMAP.md` for the phased plan, `TASK_CATALOG.md` for the exact status of every implementation unit, and `PROJECT_COMPLETION_CHECKLIST.md` for what must still exist before CCGS v2 is considered complete. The next milestone is Agent Execution Contracts (M4): a consistent input/output contract so a routed task can actually be handed to a specialist agent.
