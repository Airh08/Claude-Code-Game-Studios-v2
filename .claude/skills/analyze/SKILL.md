---
name: analyze
description: Analyze a Unity game project and update the project brain with discovered architecture, systems, dependencies, risks, and known issues.
---

# Analyze Project

This is a read-first operation. Do not modify production game code, scenes, prefabs, assets, or package configuration during analysis.

## Procedure

1. Read `CLAUDE.md` and all relevant `project-brain/` files.
2. Identify the Unity project root by looking for `Assets/`, `Packages/`, and `ProjectSettings/`.
3. If PowerShell is available, run `.claude/skills/analyze/scripts/unity-project-report.ps1 -ProjectPath <project-root>` and use its output as evidence.
4. Read `ProjectSettings/ProjectVersion.txt` to determine the actual Unity version.
5. Read `Packages/manifest.json` to identify installed packages, especially `com.unity.inputsystem`.
6. Inspect scripts, scenes, prefabs, asmdefs, tests, and relevant configuration using targeted inspection rather than dumping the whole project.
7. Identify systems and relationships. Record only evidence-based findings.
8. Invoke or emulate the `task-router` decision to recommend the smallest specialist team for the user's stated goal.
9. Update `project-brain/systems.yaml` and `project-brain/state.yaml` with discovered facts and analysis status.

## Output

Return:

- Unity version
- render/input/package findings
- project structure summary
- detected systems
- architecture overview
- dependencies
- tests discovered
- risks/issues
- recommended specialist team
- unknowns requiring confirmation

## Important

The repository containing CCGS is not necessarily the Unity game project. When CCGS is installed into a separate Unity project, analyze that project's root rather than assuming the CCGS repository itself is a Unity project.
