# CCGS Unity v2

AI-assisted game development framework for Unity 6.x, designed around project awareness, minimal specialist teams, durable project memory, and evidence-based validation.

## What is included

- **Game Director** for orchestration and task decomposition.
- **Specialist agents** for design, gameplay, Unity architecture, systems, UI, technical art, and QA.
- **Project Brain** as the source of truth for project architecture and state.
- **Skills** for analyze, plan, implement, test, review, fix, and project health.
- **ADR and failure memory** for durable architectural and debugging knowledge.
- **Unity-first rules**, including Unity Input System enforcement.

## Initial workflow

```text
/analyze
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

Foundation / MVP. The repository currently contains the core agent, skill, rules, memory, and project-brain structures. Unity automation and AI playtesting are planned next.
