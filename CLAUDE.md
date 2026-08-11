# CCGS Unity v2

Claude Code Game Studios v2 is an AI-assisted game development framework optimized for Unity 6.x.

## Mission

Help a developer move from idea to playable, tested game while preserving project coherence. Claude should understand the project before changing it, select the smallest useful team of specialists, validate changes, and update project knowledge.

## Core principles

1. Inspect before modifying.
2. Prefer the smallest agent team that can safely complete the task.
3. Keep `project-brain/` as the source of truth for project state and architecture.
4. Record important architectural decisions in `decisions/ADR/`.
5. Never claim a feature is complete without appropriate validation.
6. Use Unity Input System; do not introduce legacy `UnityEngine.Input` APIs.
7. Preserve existing architecture unless a change is explicitly justified.
8. Update documentation/state when implementation changes project behavior.

## Workflow

`/analyze` -> `/plan` -> `/implement` -> `/test` -> `/review` -> `/fix`

Not every request requires every stage, but implementation work should normally pass through analysis and validation.

## Unity baseline

- Unity: 6.3 LTS unless `project-brain/project.yaml` says otherwise.
- Language: C#.
- Input: Unity Input System.
- Target: defined by the project brain; never assume a platform when it is not documented.

## Source of truth

- `project-brain/project.yaml`: project identity and constraints.
- `project-brain/architecture.yaml`: architecture and system relationships.
- `project-brain/systems.yaml`: system inventory and ownership.
- `project-brain/state.yaml`: current implementation/health state.
- `decisions/ADR/`: durable architectural decisions.
- `memory/`: reusable lessons and known failures.

## Safety

Before destructive operations, migrations, mass edits, or dependency changes, explain the impact and prefer a reversible change. Never delete user work merely to make a test pass.
