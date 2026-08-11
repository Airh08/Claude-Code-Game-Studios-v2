using System.Text.Json;
using System.Text.Json.Serialization;

if (args.Length < 2 || args.Contains("--help", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("Usage: dotnet run -- <snapshot.json> <project-brain-directory>");
    return;
}

var snapshotPath = Path.GetFullPath(args[0]);
var brainDirectory = Path.GetFullPath(args[1]);

if (!File.Exists(snapshotPath))
{
    Console.Error.WriteLine($"Snapshot not found: {snapshotPath}");
    Environment.ExitCode = 2;
    return;
}

Directory.CreateDirectory(brainDirectory);
var snapshot = JsonSerializer.Deserialize<CcgsSnapshot>(File.ReadAllText(snapshotPath), new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});

if (snapshot is null)
{
    Console.Error.WriteLine("Snapshot could not be parsed.");
    Environment.ExitCode = 3;
    return;
}

var statePath = Path.Combine(brainDirectory, "state.yaml");
var generatedAt = DateTime.UtcNow.ToString("O");
var state = $"project:\n  status: \"analyzed\"\n  last_analyzed: \"{generatedAt}\"\n  last_validated: null\n\nscan:\n  unity_version: \"{Escape(snapshot.UnityVersion)}\"\n  scenes: {snapshot.Scenes.Count}\n  scriptable_objects: {snapshot.ScriptableObjects.Count}\n  input_action_assets: {snapshot.InputActionAssets.Count}\n  missing_components: {snapshot.Scenes.Sum(s => s.Roots.Sum(CountMissingComponents))}\n  source: \"unity-editor-snapshot\"\n\nsystems: {{}}\n\nknown_issues: []\n\nactive_work: []\n\nquality:\n  architecture: null\n  code_quality: null\n  test_coverage: null\n  performance: null\n  overall: null\n";

File.WriteAllText(statePath, state);
Console.WriteLine($"Project Brain synchronized: {statePath}");

static int CountMissingComponents(CcgsGameObjectSnapshot go)
    => go.MissingComponents.Count + go.Children.Sum(CountMissingComponents);

static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

public sealed class CcgsSnapshot
{
    public string UnityVersion { get; set; } = string.Empty;
    public List<CcgsSceneSnapshot> Scenes { get; set; } = new();
    public List<CcgsAssetSnapshot> ScriptableObjects { get; set; } = new();
    public List<string> InputActionAssets { get; set; } = new();
}

public sealed class CcgsSceneSnapshot
{
    public string Path { get; set; } = string.Empty;
    public List<CcgsGameObjectSnapshot> Roots { get; set; } = new();
}

public sealed class CcgsGameObjectSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string HierarchyPath { get; set; } = string.Empty;
    public bool Active { get; set; }
    public List<string> Components { get; set; } = new();
    public List<string> MissingComponents { get; set; } = new();
    public List<CcgsGameObjectSnapshot> Children { get; set; } = new();
}

public sealed class CcgsAssetSnapshot
{
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
