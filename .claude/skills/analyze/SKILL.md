---
name: analyze
description: Analyze a Unity game project and update the project brain with discovered architecture, systems, dependencies, risks, and known issues.
---

# Analyze Project

Before making implementation changes, inspect the project and its documentation.

## Procedure

1. Read `CLAUDE.md`.
2. Read all files under `project-brain/`.
3. Inspect Unity project metadata, packages, scripts, scenes, prefabs, and tests available in the workspace.
4. Identify systems and their relationships.
5. Detect architecture risks, missing documentation, suspicious dependencies, and known errors.
6. Update `project-brain/systems.yaml` and `project-brain/state.yaml` with evidence-based findings.
7. Do not invent facts about files or Unity objects that were not inspected.

## Output

Return:

- project summary
- detected systems
- architecture overview
- dependencies
- risks/issues
- recommended specialist team
- what remains unknown
