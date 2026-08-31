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
- Each agent's `required_validators` entry in `.claude/agents/capabilities.json` names the specific `evidence.type` values expected for that agent's domain (for example `unity-engineer` needs `scan`/`build`; `qa-engineer` needs `test`; `ui-engineer`/`technical-artist` need `manual-verification` when no automated check exists). If a required validator could not be run, say so in `unresolved_risks` instead of omitting it — never report `completed` without covering it.
- `follow_up_tasks` should be phrased so each one can become a `ccgs task create --objective "..."` call.
- `unresolved_risks` exists so partial or uncertain work stays visible instead of being silently dropped.
- An agent whose role does not produce file changes (for example `game-designer`) may leave `changes` empty and rely on `plan`/`follow_up_tasks` to carry the result.

## Escalation

If a required input is missing, the task is outside the agent's domain, or repeated attempts fail, report `status: blocked` with the reason in `summary` rather than guessing or expanding scope beyond the task.

## Execution Lifecycle

There is no separate orchestration service in this repository — the "engine" is the Claude Code session itself (typically `game-director`) driving `ccgs task`/`ccgs route` and invoking specialist agents directly. The lifecycle:

```text
ccgs task create --objective "..." [--agent <name>]
    ↓
ccgs route --task <id>              (skip if --agent was given explicitly)
    ↓
ccgs task start <task-id>           (requires the task to be routed; fails otherwise)
    ↓
invoke the routed primary agent (and any supporting agents) with the Input Contract above
    ↓
ccgs task complete <task-id> --summary "..." --evidence <type>:pass [--evidence <type>:pass ...]
  or ccgs task fail <task-id> --summary "..." [--evidence <type>:fail ...]
  or ccgs task block <task-id> --summary "..."
```

- `ccgs task start` only succeeds once the task has a `routed_agent` (from `ccgs route` or an explicit `--agent` at creation). This is what "invoking the routed agent" means concretely: the CLI records that execution began before the agent does any work, so an interrupted session leaves a visible `executing` state instead of silence.
- `ccgs task complete` enforces the Output Contract's evidence rule at the CLI level: it rejects completion without at least one `--evidence <type>:pass` entry. Use the `type` values from the agent's `required_validators` in `.claude/agents/capabilities.json` (e.g. `unity-engineer` → `scan`/`build`).
- A `failed` or `blocked` task can be restarted with `ccgs task start` (it clears the prior summary/evidence); a `completed` task cannot — treat redoing completed work as a new task.
- This is bounded-retry-free by design: nothing here limits how many times a task can be restarted or escalates automatically after repeated failures. That is M5.4, not this.
