namespace Ccgs.ProjectScanner;

public sealed record UnityValidationResult(
    IReadOnlyList<string> MissingBuildScenes,
    IReadOnlyList<string> MissingBuildSceneGuids,
    IReadOnlyList<string> InputCallbackWarnings);

public static class UnityValidation
{
    public static UnityValidationResult Validate(string root, IReadOnlyList<string> scripts)
    {
        var missingPaths = new List<string>();
        var missingGuids = new List<string>();
        var buildSettings = Path.Combine(root, "ProjectSettings", "EditorBuildSettings.asset");

        if (File.Exists(buildSettings))
        {
            var lines = File.ReadAllLines(buildSettings);
            string? currentPath = null;
            string? currentGuid = null;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("path:", StringComparison.Ordinal)) currentPath = Unquote(trimmed[5..].Trim());
                if (trimmed.StartsWith("guid:", StringComparison.Ordinal)) currentGuid = Unquote(trimmed[5..].Trim());
                if (currentPath is not null && currentGuid is not null)
                {
                    if (!File.Exists(Path.Combine(root, currentPath.Replace('/', Path.DirectorySeparatorChar))))
                        missingPaths.Add(currentPath);
                    var meta = Path.Combine(root, currentPath.Replace('/', Path.DirectorySeparatorChar) + ".meta");
                    if (File.Exists(meta) && !File.ReadAllText(meta).Contains("guid: " + currentGuid, StringComparison.Ordinal))
                        missingGuids.Add(currentGuid);
                    else if (!File.Exists(meta))
                        missingGuids.Add(currentGuid);
                    currentPath = null;
                    currentGuid = null;
                }
            }
        }

        var callbackWarnings = new List<string>();
        foreach (var script in scripts.Where(x => x.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
        {
            var absolute = Path.Combine(root, script.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolute)) continue;
            var text = File.ReadAllText(absolute);
            if (!text.Contains("OnMove", StringComparison.Ordinal) || !text.Contains("OnJump", StringComparison.Ordinal)) continue;
            if (text.Contains("InputAction.CallbackContext", StringComparison.Ordinal))
                callbackWarnings.Add($"{script}: InputAction.CallbackContext callbacks detected; verify PlayerInput notification behavior before using them with SendMessages.");
        }

        return new UnityValidationResult(missingPaths, missingGuids, callbackWarnings);
    }

    private static string Unquote(string value) => value.Trim().Trim('\"');
}
