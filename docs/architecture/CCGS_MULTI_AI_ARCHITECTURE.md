# CCGS Multi-AI Router & Ensemble — Architecture Draft

Status: PROPOSED — documentation only; no runtime integration implemented.
Date: 2026-09-17
Target: `Airh08/Claude-Code-Game-Studios-v2`, branch `feature/multi-ai-router-ensemble`.
Reference: `Donchitos/Claude-Code-Game-Studios` (read-only inspiration; no upstream changes or merges).

## 1. Objective

Extend the existing CCGS v2 workflow (`/analyze -> /plan -> /implement -> /test -> /review -> /fix`) with optional Claude/Codex task delegation and multi-perspective synthesis. Preserve the current Unity 6.x, C#, Input System, project-brain source-of-truth, ADR, and validation principles documented in `CLAUDE.md`. The smallest capable team is the default, not a mandatory ensemble.

## 2. Confirmed baseline and constraints

- V2 `CLAUDE.md` defines inspect-before-modify, minimal agent teams, `project-brain/` as project truth, `decisions/ADR/` for architecture, validation before completion, and reversible changes.
- Original CCGS documents hierarchical delegation, horizontal consultation, conflict escalation, and user approval; its `/design-review` already uses independent specialist findings and senior synthesis, while `/team-combat` has phased design, architecture, implementation, and QA. Reuse patterns, not upstream code blindly.
- Claude-to-Codex delegation through the official `openai/codex-plugin-cc` has been observed to work for context-only generation. Repository reads through the plugin were blocked in earlier tests. Direct Codex CLI approval is not proof that plugin file access works. Do not rely on repository inspection or writes by Codex until separately validated.
- No custom API key is required by this design. Do not implement undocumented plugin configuration or alter its cached files. ChatGPT-authenticated Codex is subject to applicable subscription limits; do not promise cost or speed improvements.
- This document does not establish that V2 already has the original project's complete 49-agent/73-skill system. Inventory the V2 implementation before mapping any additional roles.

## 3. Logical architecture

```text
User request / explicit skill
        |
Existing CCGS v2 workflow + project context loader
        |
Routing policy (proposed)
   |          |                |
 single     sequential      ensemble
 Claude/    hand-off        independent perspectives
 Codex          |                |
   +------------+----------------+
                |
         Synthesis (when needed)
                |
       User approval / change gate
                |
      Authorized implementation
                |
     Existing test/review/fix flow
                |
    Evidence-backed status + project-brain updates
```

The router is a decision layer, not a replacement for the v2 workflow. Ensemble is an execution mode, not a new hierarchy of 49 duplicate agents. Synthesis produces a decision proposal and records unresolved conflicts; it does not override user decisions.

## 4. Routing modes

| Mode | Trigger | Execution | Output |
|---|---|---|---|
| `single` | Narrow, low-risk, well-specified task | One eligible executor | Artifact or answer + evidence |
| `sequential` | One result depends on another | Explicit context hand-off | Traceable staged output |
| `ensemble` | Ambiguous architecture, cross-domain design, consequential trade-offs, or explicitly requested | 2–3 independent, role-specific perspectives initially | Synthesis + disagreements + verification plan |

Manual override must be possible. Start with explicit invocation in the pilot; automatic classification is a later feature after evaluating examples. Cap number of participants and rounds; do not recursively spawn ensembles.

## 5. Task contract (proposed schema)

```yaml
task_id: string
request: string
mode: single | sequential | ensemble
phase: analyze | plan | implement | test | review | fix
context:
  source_paths: []
  excerpts: []
  project_brain_revision: optional
constraints: []
acceptance_criteria: []
executor:
  role: string
  provider: claude | codex
permissions:
  read_paths: []
  write_paths: []
  requires_user_approval: true
outputs:
  format: markdown | patch | structured_report
  expected_paths: []
limits:
  max_participants: 3
  max_review_rounds: 1
```

For the Codex pilot, pass only necessary, sanitized excerpts in the prompt. Never pass `.env`, credentials, tokens, private keys, or full local settings containing secrets. A path alone is not sufficient context when plugin reads are blocked.

## 6. Ensemble protocol

1. Freeze a common task brief: goals, relevant context, constraints, acceptance criteria, and unknowns.
2. Assign genuinely different roles (e.g., gameplay design, Unity/C# architecture, QA). For competing implementations, use independent proposals without showing each agent the other's answer initially.
3. Collect attributable outputs: proposal, assumptions, evidence, trade-offs, risks, test suggestions, and `BLOCKED` reason if applicable.
4. Synthesize: compare agreements and disagreements; preserve incompatible alternatives rather than averaging them; identify decisions requiring user input.
5. Obtain approval before file changes. Only one designated implementer owns each file at a time; no parallel edits to overlapping paths.
6. Validate with real tests/compilation when available; label unexecuted checks explicitly. Record failures and unresolved issues.

No consensus-based assertion of correctness. Missing responses result in a partial report, not a fabricated synthesis.

## 7. Adapter boundary

`ClaudeAdapter`: use existing Claude Code subagent/skill mechanisms when available. `CodexAdapter`: invoke only documented official plugin entry points and collect their actual results. Adapters normalize statuses `COMPLETED`, `BLOCKED`, `FAILED`, `CANCELLED`, `UNVERIFIED`; preserve raw error messages and execution IDs when provided. Do not equate a completed plugin job with an approved or validated code review.

The initial Codex adapter is **context-only, read-only by task design**. Repository access, patch generation, and file modification are separate capabilities to test and explicitly enable later, never assumed.

## 8. Integration candidates (pending V2 inventory)

- Add an opt-in architecture/design pilot skill without changing existing `/implement` behavior.
- Reuse existing `/analyze` context and `/review` validation where their actual V2 definitions permit.
- Use original CCGS `/design-review`, `/team-combat`, and `/code-review` as conceptual reference only; do not assume those exact skills exist in V2.
- Keep `project-brain/` authoritative for project state and `decisions/ADR/` for accepted decisions. Proposed ensemble output is not an accepted ADR until approved.

## 9. Safety and observability

- No modifications to `main`, upstream Donchitos repo, plugin cache, or local working tree as part of this document.
- Explicit approval for writes and multi-file changes; never silently execute destructive commands or commit.
- Log task ID, mode, role/provider, status, output references, validation evidence, and blockers without storing secrets or unnecessary prompt contents.
- Preserve user edits and distinguish remote branch state from local uncommitted changes.
- If Codex file inspection is blocked, return `BLOCKED` and fall back to sanitized explicit context or ask the user; never weaken sandbox settings automatically.

## 10. Phased roadmap and acceptance criteria

### Phase 0 — V2 inventory (next)
Inspect `.claude/agents`, `.claude/skills`, `.claude/hooks`, `.claude/rules`, `tools/`, existing tests, project-brain schemas, and roadmap. Produce a component map and precise integration points. No implementation before inventory.

### Phase 1 — Contract and manual pilot
Define validated task/result contracts and an explicit opt-in skill for a Unity ability design. Run Claude and Codex on the same sanitized brief; collect distinct perspectives and synthesize without editing files. Verify blocked/error/partial-result handling.

### Phase 2 — Review and validation
Integrate existing V2 review/test facilities, enforce approval and exclusive file ownership, and record evidence. Test one successful path, one blocked Codex read, one disagreement, and one failed validation.

### Phase 3 — Optional routing automation
Only after pilot evidence, add documented policy rules for single/sequential/ensemble selection, participant/round limits, and a manual override. Compare quality, latency, and usage empirically; make no unsupported optimization claims.

### Definition of done for the pilot
- Existing workflows remain functional and `main` is untouched.
- Explicit mode selection works; all participants receive the same relevant constraints.
- Distinct agent outputs and unresolved disagreements remain attributable.
- No writes occur without approval; no overlapping concurrent file edits.
- Blocked Codex access produces a truthful partial report.
- Test outcomes distinguish executed/pass/fail/not-run; user receives a reproducible report.

## 11. Open questions

- Which V2 skills and agents are actually present and callable in the branch?
- Which plugin invocation paths can be reliably used from the V2 runtime?
- Which Unity test/build commands exist for the user's local project, and which are safe to run?
- What approval checkpoints and participant limits does the user prefer?

Resolve these through Phase 0 inventory and a small pilot, not assumptions.
