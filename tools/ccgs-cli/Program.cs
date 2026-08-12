using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ccgs.Cli;

if (args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase))
{
    PrintHelp();
    return;
}

var command = args[0].ToLowerInvariant();
var projectRoot = args.Length > 1 && !args[1].StartsWith("--", StringComparison.Ordinal)
    ? Path.GetFullPath(args[1])
    : Directory.GetCurrentDirectory();

switch (command)
{
    case "scan":
        RunScan(projectRoot, args.Contains("--pretty", StringComparer.OrdinalIgnoreCase));
        break;
    case "analyze":
        RunAnalyze(projectRoot, args.Contains("--pretty", StringComparer.OrdinalIgnoreCase));
        break;
    default:
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        Environment.ExitCode = 1;
        break;
}

static void RunScan(string root, bool pretty)
{
    ValidateProjectRoot(root);
    var scanner = LocateScanner();
    var result = RunProcess(scanner, $"\"{root}\" {(pretty ? "--pretty" : "")}");
    Console.Write(result);
}

static void RunAnalyze(string root, bool pretty)
{
    ValidateProjectRoot(root);
    var scanner = LocateScanner();
    var json = RunProcess(scanner, $"\"{root}\" --pretty");
    using var document = JsonDocument.Parse(json);
    var scan = document.RootElement.Clone();
    var report = new AnalysisReport
    {
        GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
        ProjectRoot = root,
        Scanner = scan,
        Health = HealthReport.FromScan(scan),
        Recommendations = BuildRecommendations(scan)
    };

    var options = new JsonSerializerOptions
    {
        WriteIndented = pretty,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    Console.WriteLine(JsonSerializer.Serialize(report, options));
}

static List<string> BuildRecommendations(JsonElement scan)
{
    var recommendations = new List<string>();
    if (!scan.GetProperty("IsUnityProject").GetBoolean())
        recommendations.Add("Verify the selected directory is a Unity project root.");
    if (!scan.GetProperty("HasInputSystem").GetBoolean())
        recommendations.Add("Review input architecture; Unity Input System was not detected.");
    if (scan.GetProperty("MissingBuildScenes").GetArrayLength() > 0)
        recommendations.Add("Fix Build Settings entries that reference non-existent scene paths.");
    if (scan.GetProperty("InputCallbackWarnings").GetArrayLength() > 0)
        recommendations.Add("Inspect PlayerInput notification behavior in the Unity Editor before changing input callbacks.");
    if (scan.GetProperty("TestDirectories").GetArrayLength() == 0)
        recommendations.Add("Establish baseline automated tests.");
    return recommendations;
}

static string LocateScanner()
{
    var candidates = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "project-scanner"),
        Path.Combine(Directory.GetCurrentDirectory(), "tools", "project-scanner")
    };

    var project = candidates.Select(Path.GetFullPath).FirstOrDefault(x => File.Exists(Path.Combine(x, "ProjectScanner.csproj")));
    if (project is null)
        throw new InvalidOperationException("Could not locate tools/project-scanner/ProjectScanner.csproj.");
    return project;
}

static string RunProcess(string workingDirectory, string arguments)
{
    var psi = new ProcessStartInfo("dotnet", $"run --project \"{Path.Combine(workingDirectory, "ProjectScanner.csproj")}\" -- {arguments}")
    {
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start scanner process.");
    var output = process.StandardOutput.ReadToEnd();
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0) throw new InvalidOperationException($"Scanner failed: {error}");
    return output;
}

static void ValidateProjectRoot(string root)
{
    if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
}

static void PrintHelp()
{
    Console.WriteLine("CCGS CLI");
    Console.WriteLine("Usage: ccgs <command> <unity-project-root> [--pretty]");
    Console.WriteLine("Commands:");
    Console.WriteLine("  scan     Run deterministic filesystem inspection");
    Console.WriteLine("  analyze  Run inspection and produce a structured health report");
}

public sealed class AnalysisReport
{
    public string GeneratedAtUtc { get; set; } = string.Empty;
    public string ProjectRoot { get; set; } = string.Empty;
    public JsonElement Scanner { get; set; }
    public HealthReport Health { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}
