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
var pretty = args.Contains("--pretty", StringComparer.OrdinalIgnoreCase);
var syncBrain = args.Contains("--sync-brain", StringComparer.OrdinalIgnoreCase);

switch (command)
{
    case "scan":
        RunScan(projectRoot, pretty);
        break;
    case "analyze":
        RunAnalyze(projectRoot, pretty, syncBrain);
        break;
    case "task":
        RunTask(args, pretty);
        break;
    case "route":
        RunRoute(args, projectRoot, pretty);
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

static void RunAnalyze(string root, bool pretty, bool syncBrain)
{
    ValidateProjectRoot(root);
    var scanner = LocateScanner();
    var json = RunProcess(scanner, $"\"{root}\" --pretty");
    using var document = JsonDocument.Parse(json);
    var scan = document.RootElement.Clone();
    var health = HealthReport.FromScan(scan);
    var report = new AnalysisReport
    {
        GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
        ProjectRoot = root,
        Scanner = scan,
        Health = health,
        Recommendations = BuildRecommendations(scan)
    };

    if (syncBrain)
        report.BrainPath = BrainSync.Sync(root, scan, health);

    var options = new JsonSerializerOptions
    {
        WriteIndented = pretty,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    Console.WriteLine(JsonSerializer.Serialize(report, options));
}

static void RunTask(string[] args, bool pretty)
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Usage: ccgs task <create|list> <unity-project-root> [options]");
        Environment.ExitCode = 1;
        return;
    }

    var subcommand = args[1].ToLowerInvariant();
    var root = Path.GetFullPath(args[2]);
    ValidateProjectRoot(root);
    var brainDir = Path.Combine(root, "project-brain");

    switch (subcommand)
    {
        case "create":
            var objective = GetValue(args, "--objective");
            if (string.IsNullOrWhiteSpace(objective))
            {
                Console.Error.WriteLine("--objective is required.");
                Environment.ExitCode = 1;
                return;
            }

            var request = new TaskCreateRequest(
                Objective: objective,
                Type: GetValue(args, "--type") ?? "implementation",
                Priority: GetValue(args, "--priority") ?? "medium",
                AssignedAgent: GetValue(args, "--agent"),
                AffectedPaths: GetValues(args, "--path"),
                Constraints: GetValues(args, "--constraint"),
                Dependencies: GetValues(args, "--depends-on"),
                ValidationRequirements: GetValues(args, "--validation"),
                Source: GetValue(args, "--source") ?? "user");

            var created = TaskStore.Create(brainDir, request);
            var createOptions = new JsonSerializerOptions { WriteIndented = pretty };
            Console.WriteLine(JsonSerializer.Serialize(created, createOptions));
            break;
        case "list":
            var tasks = TaskStore.Load(brainDir);
            var listOptions = new JsonSerializerOptions { WriteIndented = pretty };
            Console.WriteLine(JsonSerializer.Serialize(tasks, listOptions));
            break;
        default:
            Console.Error.WriteLine($"Unknown task subcommand: {subcommand}");
            Environment.ExitCode = 1;
            break;
    }
}

static void RunRoute(string[] args, string root, bool pretty)
{
    ValidateProjectRoot(root);
    var issueCode = GetValue(args, "--issue");
    var taskId = GetValue(args, "--task");

    if (string.IsNullOrWhiteSpace(issueCode) && string.IsNullOrWhiteSpace(taskId))
    {
        Console.Error.WriteLine("Provide --issue <code> or --task <task-id>.");
        Environment.ExitCode = 1;
        return;
    }

    RoutingDecision decision;
    string subjectType;
    string subjectId;
    var subjectFacts = new Dictionary<string, string>();
    string generatedAtUtc;
    bool persisted;

    if (!string.IsNullOrWhiteSpace(taskId))
    {
        var brainDir = Path.Combine(root, "project-brain");
        var tasks = TaskStore.Load(brainDir);
        var task = tasks.FirstOrDefault(t => string.Equals(t.Id, taskId, StringComparison.OrdinalIgnoreCase));
        if (task is null)
        {
            Console.Error.WriteLine($"Task not found: {taskId}");
            Environment.ExitCode = 1;
            return;
        }
        decision = TaskRouter.RouteTask(task);
        subjectType = "task";
        subjectId = task.Id;
        subjectFacts["objective"] = task.Objective;
        subjectFacts["type"] = task.Type;
        subjectFacts["priority"] = task.Priority;

        var routed = TaskStore.SaveRouting(brainDir, task.Id, decision);
        generatedAtUtc = routed.RoutedAtUtc!;
        persisted = true;
    }
    else
    {
        decision = TaskRouter.RouteIssueCode(issueCode!);
        subjectType = "issue";
        subjectId = issueCode!;
        subjectFacts["code"] = issueCode!;
        generatedAtUtc = DateTime.UtcNow.ToString("O");
        persisted = false;
    }

    var artifact = new RoutingArtifact
    {
        SubjectType = subjectType,
        SubjectId = subjectId,
        SubjectFacts = subjectFacts,
        PrimaryAgent = decision.PrimaryAgent,
        SupportingAgents = decision.SupportingAgents.ToList(),
        MatchedRule = decision.MatchedRule,
        Rationale = decision.Rationale,
        PersistedToBrain = persisted,
        GeneratedAtUtc = generatedAtUtc
    };

    var options = new JsonSerializerOptions { WriteIndented = pretty, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    Console.WriteLine(JsonSerializer.Serialize(artifact, options));
}

static List<string> GetValues(string[] args, string flag)
{
    var values = new List<string>();
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
            values.Add(args[i + 1]);
    }
    return values;
}

static string? GetValue(string[] args, string flag) => GetValues(args, flag).FirstOrDefault();

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
    Console.WriteLine("Usage: ccgs <command> <unity-project-root> [--pretty] [--sync-brain]");
    Console.WriteLine("Commands:");
    Console.WriteLine("  scan          Run deterministic filesystem inspection");
    Console.WriteLine("  analyze       Run inspection and produce a structured health report");
    Console.WriteLine("  task create   Persist a new task to <project>/project-brain/tasks.yaml");
    Console.WriteLine("  task list     List tasks persisted in <project>/project-brain/tasks.yaml");
    Console.WriteLine("  route         Resolve the agent(s) for a task or Brain issue code");
    Console.WriteLine("Options:");
    Console.WriteLine("  --sync-brain  Persist observed analysis into <project>/project-brain");
    Console.WriteLine("Task create options:");
    Console.WriteLine("  --objective \"...\"    Required. What the task must accomplish.");
    Console.WriteLine("  --type <type>         analysis|design|implementation|debugging|refactor|content|ui|technical-art|testing|production (default: implementation)");
    Console.WriteLine("  --priority <priority> low|medium|high|critical (default: medium)");
    Console.WriteLine("  --agent <name>        Assigned specialist agent");
    Console.WriteLine("  --path <path>         Affected path (repeatable)");
    Console.WriteLine("  --constraint \"...\"    Constraint (repeatable)");
    Console.WriteLine("  --depends-on <id>     Dependency task id (repeatable)");
    Console.WriteLine("  --validation \"...\"    Validation requirement (repeatable)");
    Console.WriteLine("Route options:");
    Console.WriteLine("  --task <task-id>      Route an existing task from project-brain/tasks.yaml (persists the decision back into the task record)");
    Console.WriteLine("  --issue <code>        Route a Brain health issue code (e.g. BUILD-001); not persisted, no task record to attach it to");
}

public sealed class AnalysisReport
{
    public string GeneratedAtUtc { get; set; } = string.Empty;
    public string ProjectRoot { get; set; } = string.Empty;
    public JsonElement Scanner { get; set; }
    public HealthReport Health { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public string? BrainPath { get; set; }
}

public sealed class RoutingArtifact
{
    public string SubjectType { get; set; } = string.Empty;
    public string SubjectId { get; set; } = string.Empty;
    public Dictionary<string, string> SubjectFacts { get; set; } = new();
    public string? PrimaryAgent { get; set; }
    public List<string> SupportingAgents { get; set; } = new();
    public string MatchedRule { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public bool PersistedToBrain { get; set; }
    public string GeneratedAtUtc { get; set; } = string.Empty;
}
