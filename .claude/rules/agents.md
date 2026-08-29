# Agent Contracts

Applies to every agent in `.claude/agents/*.md` — orchestration (`game-director`) and specialists alike.

## Input Contract

Every agent invoked for a task should receive:

- `task`: the routed task record when one exists (`project-brain/tasks.yaml`) — `id`, `objective`, `type`, `priority`, `affected_paths`, `constraints`, `dependencies`, `validation_requirements`.
- `routing`: how the agent was assigned — `matched_rule`, `rationale`, `supporting_agents` (from `ccgs route` when the task went through the Task Router).
- `brain_context`: the relevant slice of Project Brain needed to act safely — current health (`project-brain/state.yaml`), relevant open issues (`project-brain/issues.yaml`), and project/architecture facts (`project-brain/project.yaml`, `project-brain/architecture.yaml`).
- `relevant_files`: the files/paths the agent is expected to inspect or modify, scoped to `affected_paths` unless investigation shows more is needed.

If one of these is missing, ask for it or inspect the project to reconstruct it — do not invent project state. This is the same "inspect before modifying" principle as `CLAUDE.md`, made concrete per input.

## Output Contract

Report results in this shape instead of free prose, so the Game Director or another agent can consume them without re-parsing narrative text:

```yaml
result:
  task_id: ""
  status: completed|blocked|failed
  summary: ""
  plan: []
  changes:
    - path: ""
      description: ""
  evidence:
    - type: test|build|manual-verification|scan
      description: ""
      result: pass|fail|unverified
  tests:
    - name: ""
      status: pass|fail|not-run
  unresolved_risks: []
  follow_up_tasks: []
```

- `status: completed` requires at least one `evidence` entry with `result: pass` relevant to the task's `validation_requirements`. Do not report `completed` on assertion alone — see `.claude/rules/testing.md`.
- `follow_up_tasks` should be phrased so each one can become a `ccgs task create --objective "..."` call.
- `unresolved_risks` exists so partial or uncertain work stays visible instead of being silently dropped.
- An agent whose role does not produce file changes (for example `game-designer`) may leave `changes` empty and rely on `plan`/`follow_up_tasks` to carry the result.

## Escalation

If a required input is missing, the task is outside the agent's domain, or repeated attempts fail, report `status: blocked` with the reason in `summary` rather than guessing or expanding scope beyond the task.
