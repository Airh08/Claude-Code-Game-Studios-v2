using System.Text.Json;
using System.Text.Json.Serialization;

if (args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("Usage: dotnet run -- <unity-project-root> [--pretty]");
    return;
}

var root = Path.GetFullPath(args[0]);
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"Project root does not exist: {root}");
    Environment.ExitCode = 2;
    return;
}

var report = UnityProjectScanner.Scan(root);
var options = new JsonSerializerOptions
{
    WriteIndented = args.Contains("--pretty", StringComparer.OrdinalIgnoreCase),
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
Console.WriteLine(JsonSerializer.Serialize(report, options));

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
    IReadOnlyList<string> Warnings);

public static class UnityProjectScanner
{
    public static UnityProjectReport Scan(string root)
    {
        var projectVersionPath = Path.Combine(root, "ProjectSettings", "ProjectVersion.txt");
        var manifestPath = Path.Combine(root, "Packages", "manifest.json");
        var assetsPath = Path.Combine(root, "Assets");

        var warnings = new List<string>();
        var isUnity = Directory.Exists(assetsPath) && Directory.Exists(Path.Combine(root, "ProjectSettings"));

        var unityVersion = File.Exists(projectVersionPath)
            ? ReadUnityVersion(File.ReadAllLines(projectVersionPath))
            : null;

        var packageText = File.Exists(manifestPath) ? File.ReadAllText(manifestPath) : string.Empty;
        var hasInputSystem = packageText.Contains("com.unity.inputsystem", StringComparison.OrdinalIgnoreCase);

        if (!isUnity) warnings.Add("Assets and/or ProjectSettings directory not found; this may not be a Unity project.");
        if (unityVersion is null) warnings.Add("Unity editor version could not be determined.");
        if (File.Exists(manifestPath) && !hasInputSystem) warnings.Add("Unity Input System package was not detected in Packages/manifest.json.");
        if (!File.Exists(manifestPath)) warnings.Add("Packages/manifest.json not found.");

        return new UnityProjectReport(
            root,
            unityVersion,
            isUnity,
            hasInputSystem,
            FindFiles(root, "Packages", "*.json"),
            FindFiles(root, "Assets", "*.cs"),
            FindFiles(root, "Assets", "*.unity"),
            FindFiles(root, "Assets", "*.prefab"),
            FindFiles(root, "Assets", "*.asmdef"),
            FindTestDirectories(root),
            warnings);
    }

    private static string? ReadUnityVersion(IEnumerable<string> lines)
    {
        const string prefix = "m_EditorVersion:";
        var line = lines.FirstOrDefault(x => x.StartsWith(prefix, StringComparison.Ordinal));
        return line?[prefix.Length..].Trim();
    }

    private static List<string> FindFiles(string root, string relativeDirectory, string pattern)
    {
        var directory = Path.Combine(root, relativeDirectory);
        if (!Directory.Exists(directory)) return [];

        return Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories)
            .Select(x => Path.GetRelativePath(root, x).Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> FindTestDirectories(string root)
    {
        var assets = Path.Combine(root, "Assets");
        if (!Directory.Exists(assets)) return [];

        return Directory.EnumerateDirectories(assets, "*", SearchOption.AllDirectories)
            .Where(x => Path.GetFileName(x).Contains("test", StringComparison.OrdinalIgnoreCase))
            .Select(x => Path.GetRelativePath(root, x).Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
