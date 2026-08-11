# CCGS Project Brain Sync

Transforms deterministic scanner/editor snapshot data into the durable `project-brain/` representation.

## Principles

- Read-only source inspection.
- Deterministic output from the same snapshot.
- Never invent project facts.
- Preserve human-authored fields unless the synchronizer owns them.
- Record source and timestamp metadata.

The first implementation uses JSON snapshots as the interchange contract. A later phase can add direct YAML writing and schema validation.
