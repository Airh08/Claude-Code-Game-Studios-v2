# ADR-001: CCGS v2 Core Architecture

- Status: accepted
- Date: 2026-08-11

## Context

The original CCGS approach provides a large catalog of agents and skills. CCGS v2 needs to remain useful as a long-lived game project grows without requiring every task to invoke many specialists.

## Decision

CCGS v2 uses four core layers:

1. **Project Brain** — durable project facts, architecture, systems, and state.
2. **Game Director** — coordinates goals and cross-system decisions.
3. **Task Router** — selects the smallest useful specialist team.
4. **Validation loop** — test, review, and fix before completion.

Specialist agents remain modular and are selected per task rather than all being active for every request.

## Consequences

### Positive

- Lower unnecessary context and agent usage.
- Clearer ownership and orchestration.
- Better long-term project consistency.
- Easier to add specialists without changing the whole workflow.

### Trade-offs

- The Project Brain must be kept accurate.
- Routing quality becomes important.
- Some complex tasks require explicit escalation to the Game Director.
