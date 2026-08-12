# Project Brain

The Project Brain is CCGS's persistent project knowledge layer.

## Principles

- **Observed facts** come from deterministic scanners and Unity Editor inspection.
- **Derived knowledge** comes from agents and must retain provenance.
- **Human decisions** are explicit and should not be silently overwritten by automation.

The initial Brain schema is intentionally small. It will grow only when a concrete workflow requires new state.

## Files

- `project.yaml` — stable project facts discovered from analysis.
- `state.yaml` — current analysis/health/work state.
- `issues.yaml` — persistent issues with identity and lifecycle state.
- `architecture.yaml` — conservative architectural knowledge; unknowns remain unknown until supported by evidence.

## Synchronization

`ccgs analyze` can synchronize the deterministic analysis into this directory with:

```text
ccgs analyze <unity-project-root> --sync-brain
```

Brain synchronization is additive for observed facts and preserves existing issue lifecycle state where issue identity can be matched.
