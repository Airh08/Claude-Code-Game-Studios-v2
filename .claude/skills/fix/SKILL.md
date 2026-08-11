---
name: fix
description: Diagnose and fix a reported failure or review finding while minimizing unrelated changes and preserving evidence.
---

# Fix

## Procedure

1. Reproduce or inspect the reported failure.
2. Identify root cause rather than patching symptoms.
3. Check project memory for prior failures.
4. Make the smallest safe correction.
5. Re-run the relevant validation.
6. Update `memory/failures/` when the lesson is reusable.
7. Update project brain if the fix changes architecture or behavior.

If the failure cannot be reproduced, state that explicitly and distinguish diagnosis from verification.
