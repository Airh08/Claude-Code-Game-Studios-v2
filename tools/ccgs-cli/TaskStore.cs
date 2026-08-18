using System.Text;
using System.Text.Json;

namespace Ccgs.Cli;

public sealed class BrainTask
{
    public string Id { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public string Status { get; set; } = "open";
    public string? AssignedAgent { get; set; }
    public List<string> AffectedPaths { get; set; } = new();
    public List<string> Constraints { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public List<string> ValidationRequirements { get; set; } = new();
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;
    public string Source { get; set; } = "user";
}

public sealed record TaskCreateRequest(
    string Objective,
    string Type,
    string Priority,
    string? AssignedAgent,
    IReadOnlyList<string> AffectedPaths,
    IReadOnlyList<string> Constraints,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> ValidationRequirements,
    string Source);

public static class TaskStore
{
    private static readonly string[] ListFields = { "affected_paths", "constraints", "dependencies", "validation_requirements" };

    public static BrainTask Create(string brainDir, TaskCreateRequest request)
    {
        Directory.CreateDirectory(brainDir);
        var file = Path.Combine(brainDir, "tasks.yaml");
        var tasks = File.Exists(file) ? Read(File.ReadAllLines(file)) : new List<BrainTask>();

        var now = DateTime.UtcNow.ToString("O");
        var task = new BrainTask
        {
            Id = $"TASK-{Guid.NewGuid():N}".Substring(0, 13).ToUpperInvariant(),
            Objective = request.Objective,
            Type = request.Type,
            Priority = request.Priority,
            Status = "open",
            AssignedAgent = request.AssignedAgent,
            AffectedPaths = request.AffectedPaths.ToList(),
            Constraints = request.Constraints.ToList(),
            Dependencies = request.Dependencies.ToList(),
            ValidationRequirements = request.ValidationRequirements.ToList(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Source = request.Source
        };

        tasks.Add(task);
        Write(file, tasks);
        return task;
    }

    public static List<BrainTask> Load(string brainDir)
    {
        var file = Path.Combine(brainDir, "tasks.yaml");
        return File.Exists(file) ? Read(File.ReadAllLines(file)) : new List<BrainTask>();
    }

    private static void Write(string file, IEnumerable<BrainTask> tasks)
    {
        var builder = new StringBuilder("schema_version: 1\ntasks:\n");
        foreach (var task in tasks)
        {
            AppendLine(builder, $"  - id: {task.Id}");
            AppendLine(builder, $"    objective: \"{Yaml(task.Objective)}\"");
            AppendLine(builder, $"    type: {task.Type}");
            AppendLine(builder, $"    priority: {task.Priority}");
            AppendLine(builder, $"    status: {task.Status}");
            if (!string.IsNullOrWhiteSpace(task.AssignedAgent))
                AppendLine(builder, $"    assigned_agent: {task.AssignedAgent}");
            WriteList(builder, "affected_paths", task.AffectedPaths);
            WriteList(builder, "constraints", task.Constraints);
            WriteList(builder, "dependencies", task.Dependencies);
            WriteList(builder, "validation_requirements", task.ValidationRequirements);
            AppendLine(builder, $"    created_at_utc: \"{task.CreatedAtUtc}\"");
            AppendLine(builder, $"    updated_at_utc: \"{task.UpdatedAtUtc}\"");
            AppendLine(builder, $"    source: {task.Source}");
        }
        File.WriteAllText(file, builder.ToString());
    }

    private static void WriteList(StringBuilder builder, string key, List<string> values)
    {
        if (values.Count == 0)
        {
            AppendLine(builder, $"    {key}: []");
            return;
        }

        AppendLine(builder, $"    {key}:");
        foreach (var value in values)
            AppendLine(builder, $"      - \"{Yaml(value)}\"");
    }

    private static List<BrainTask> Read(string[] lines)
    {
        var result = new List<BrainTask>();
        BrainTask? current = null;
        string? activeListField = null;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();

            if (line.StartsWith("  - id:", StringComparison.Ordinal))
            {
                if (current is not null)
                    result.Add(current);
                current = new BrainTask { Id = line[8..].Trim() };
                activeListField = null;
                continue;
            }

            if (current is null)
                continue;

            if (activeListField is not null)
            {
                if (line.StartsWith("      - ", StringComparison.Ordinal))
                {
                    GetList(current, activeListField).Add(Unquote(line[8..].Trim()));
                    continue;
                }
                activeListField = null;
            }

            if (!line.StartsWith("    ", StringComparison.Ordinal))
                continue;

            var fieldSeparator = line.IndexOf(':', 4);
            if (fieldSeparator < 0)
                continue;

            var field = line[4..fieldSeparator];
            var rawValue = line[(fieldSeparator + 1)..].Trim();

            if (Array.IndexOf(ListFields, field) >= 0)
            {
                if (rawValue == "[]" || rawValue.Length == 0)
                {
                    activeListField = rawValue.Length == 0 ? field : null;
                    continue;
                }
            }

            var fieldValue = Unquote(rawValue);
            switch (field)
            {
                case "objective": current.Objective = fieldValue; break;
                case "type": current.Type = fieldValue; break;
                case "priority": current.Priority = fieldValue; break;
                case "status": current.Status = fieldValue; break;
                case "assigned_agent": current.AssignedAgent = fieldValue; break;
                case "created_at_utc": current.CreatedAtUtc = fieldValue; break;
                case "updated_at_utc": current.UpdatedAtUtc = fieldValue; break;
                case "source": current.Source = fieldValue; break;
            }
        }

        if (current is not null)
            result.Add(current);

        return result;
    }

    private static List<string> GetList(BrainTask task, string field) => field switch
    {
        "affected_paths" => task.AffectedPaths,
        "constraints" => task.Constraints,
        "dependencies" => task.Dependencies,
        "validation_requirements" => task.ValidationRequirements,
        _ => throw new InvalidOperationException($"Unknown list field: {field}")
    };

    private static void AppendLine(StringBuilder builder, string line)
    {
        builder.Append(line).Append('\n');
    }

    private static string Yaml(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            try { return JsonSerializer.Deserialize<string>(value) ?? string.Empty; }
            catch (JsonException) { }
        }
        return value;
    }
}
