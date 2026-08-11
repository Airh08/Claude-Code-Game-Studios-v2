# Hooks

Hooks will provide deterministic guardrails around edits, tests, and state synchronization.

Planned MVP hooks:

- pre-edit: validate protected paths and project context.
- post-edit: detect relevant project-brain drift.
- post-test: record validation evidence.

Do not add shell hooks until their behavior is defined and tested for the target Claude Code environment.
