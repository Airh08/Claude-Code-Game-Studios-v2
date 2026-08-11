---
name: review
description: Review implementation changes for correctness, architecture consistency, regressions, maintainability, and project brain drift.
---

# Review

Review the diff and relevant context, not just the final files.

Check:

- correctness against acceptance criteria
- architecture and coupling
- Unity lifecycle and serialization risks
- Input System compatibility
- null/reference safety
- test adequacy
- unrelated changes
- documentation/project-brain drift

Classify findings as blocker, major, minor, or suggestion. A clean review means no known blocker/major issue, not that the code is perfect.
