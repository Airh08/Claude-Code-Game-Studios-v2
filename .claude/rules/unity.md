# Unity Rules

- Baseline: Unity 6.3 LTS unless project metadata says otherwise.
- Use the Unity Input System; do not introduce legacy input APIs.
- Protect serialized references in scenes and prefabs.
- Consider Unity lifecycle ordering before adding initialization dependencies.
- Validate package APIs against the installed project version.
- Prefer ScriptableObjects or data assets when the project architecture benefits from data-driven configuration.
