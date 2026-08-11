# CCGS Unity Editor Scanner

Unity Editor-side inspection layer for information that filesystem scanning cannot safely determine.

## Planned snapshot

The scanner will inspect, in read-only mode:

- scenes and root GameObjects
- component types
- missing scripts/components
- prefab instances and overrides
- ScriptableObjects
- Animator Controllers and state machines
- serialized object references
- Input Actions and action maps
- build scenes

## Safety

The scanner must never modify scenes, prefabs, assets, packages, or project settings. Its job is to observe and serialize a snapshot.

The implementation is intentionally kept as an Editor-only assembly so runtime builds do not include CCGS tooling.
