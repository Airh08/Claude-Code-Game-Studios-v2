# CCGS CLI

The CCGS command-line entry point. It orchestrates deterministic tooling and keeps AI interpretation separate from filesystem inspection.

## Commands

```text
ccgs analyze <unity-project>
ccgs scan <unity-project>
ccgs sync <snapshot.json> <project-brain>
```

The first milestone is `analyze`: validate the project root, run the filesystem scanner, optionally consume an existing Unity Editor snapshot, and emit a stable analysis artifact.

AI agent execution is intentionally not embedded in the scanner itself. Claude Code remains the reasoning/orchestration layer.
