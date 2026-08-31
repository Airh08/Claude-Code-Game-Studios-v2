using System.Text.Json;

namespace Ccgs.Cli;

public sealed class AgentCapability
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "specialist";
    public List<string> Domains { get; set; } = new();
    public List<string> SupportedTaskTypes { get; set; } = new();
    public string ReadWriteScope { get; set; } = "read-write";
    public List<string> RequiredValidators { get; set; } = new();
}

public sealed class IssueCodeRule
{
    public string Code { get; set; } = string.Empty;
    public string? Primary { get; set; }
    public List<string> Supporting { get; set; } = new();
    public string Rationale { get; set; } = string.Empty;
}

public sealed class TaskTypeRule
{
    public string Type { get; set; } = string.Empty;
    public string? Primary { get; set; }
    public List<string> Supporting { get; set; } = new();
    public string Rationale { get; set; } = string.Empty;
}

public sealed class AgentCapabilityRegistry
{
    public int SchemaVersion { get; set; } = 1;
    public List<AgentCapability> Agents { get; set; } = new();
    public List<IssueCodeRule> IssueCodeRules { get; set; } = new();
    public List<TaskTypeRule> TaskTypeRules { get; set; } = new();
}

public static class AgentCapabilities
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static AgentCapabilityRegistry Load(string? explicitPath = null)
    {
        var path = explicitPath ?? Locate();
        if (path is null || !File.Exists(path))
            throw new InvalidOperationException("Could not locate .claude/agents/capabilities.json.");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AgentCapabilityRegistry>(json, Options)
            ?? throw new InvalidOperationException($"{path} did not deserialize to a valid agent capability registry.");
    }

    private static string? Locate()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".claude", "agents", "capabilities.json"),
            Path.Combine(Directory.GetCurrentDirectory(), ".claude", "agents", "capabilities.json")
        };
        return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
    }
}
