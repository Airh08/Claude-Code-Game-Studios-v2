---
name: test
description: Validate a Unity change using the strongest available automated and project-level evidence, then report pass, fail, and unverified areas.
---

# Test

## Procedure

1. Read the feature acceptance criteria.
2. Identify affected edit-mode, play-mode, integration, or build tests.
3. Run the most relevant available tests.
4. Inspect compiler errors, Unity logs, and test failures.
5. For input-related changes, validate action bindings and callback signatures.
6. Record exact commands/results when possible.
7. Update `project-brain/state.yaml` with validation status.

Never convert an unrun test into a passing claim.
