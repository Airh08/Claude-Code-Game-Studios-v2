namespace Ccgs.ProjectScanner;

public sealed record UnityProjectReport(
    string ProjectRoot,
    string? UnityVersion,
    bool IsUnityProject,
    bool HasInputSystem,
    IReadOnlyList<string> Packages,
    IReadOnlyList<string> Scripts,
    IReadOnlyList<string> Scenes,
    IReadOnlyList<string> Prefabs,
    IReadOnlyList<string> AssemblyDefinitions,
    IReadOnlyList<string> TestDirectories,
    IReadOnlyList<string> MissingBuildScenes,
    IReadOnlyList<string> MissingBuildSceneGuids,
    IReadOnlyList<string> InputCallbackWarnings,
    IReadOnlyList<string> Warnings);
