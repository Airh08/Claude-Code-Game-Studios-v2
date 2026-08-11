# CCGS Project Scanner

The scanner is the deterministic inspection layer for CCGS. It should eventually produce a machine-readable project inventory that the Project Brain and Task Router can consume.

## Contract

Input: Unity project root.

Output: JSON containing:

- Unity version
- package inventory
- Input System detection
- C# script inventory
- scenes
- prefabs
- assembly definitions
- test locations
- basic project warnings

## Design

Keep scanning deterministic and read-only. Do not execute game code, modify assets, or infer behavior from names alone.

The current implementation target is a cross-platform .NET/C# console tool. Unity Editor integration can be added later for serialized-object and scene-level inspection that filesystem scanning cannot safely provide.
