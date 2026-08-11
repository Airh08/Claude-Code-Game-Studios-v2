---
name: implement
description: Implement an approved feature plan using the minimum appropriate specialist team while preserving project architecture and project brain consistency.
---

# Implement Feature

## Preconditions

- Read `CLAUDE.md`.
- Read the relevant project brain files.
- Have a plan or clearly scoped task.
- Identify affected systems before editing.

## Procedure

1. Inspect existing implementation and tests.
2. Make the smallest coherent change that satisfies the plan.
3. Follow Unity 6.x and project-specific conventions.
4. Preserve serialized references and scene/prefab integrity.
5. Add or update tests where practical.
6. Update project brain documentation when behavior, ownership, or dependencies change.
7. Hand off to `/test` and `/review` before declaring completion.

Do not silently rewrite unrelated systems.
