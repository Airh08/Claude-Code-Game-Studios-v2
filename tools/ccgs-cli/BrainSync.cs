using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ccgs.Cli;

public static class BrainSync
{
    public static string Sync(string projectRoot, JsonElement scan, HealthReport health)
    {
        var dir = Path.Combine(projectRoot, "project-brain");
        Directory.CreateDirectory(dir);
        var now = DateTime.UtcNow.ToString("O");

        var unity = scan.GetProperty("UnityVersion").GetString() ?? "unknown";
        var input = scan.GetProperty("HasInputSystem").GetBoolean();
        var scripts = scan.GetProperty("Scripts").GetArrayLength();
        var scenes = scan.GetProperty("Scenes").GetArrayLength();
        var prefabs = scan.GetProperty("Prefabs").GetArrayLength();
        var tests = scan.GetProperty("TestDirectories").GetArrayLength();

        File.WriteAllText(Path.Combine(dir, "project.yaml"), $"schema_version: 1\nproject:\n  name: \"{Yaml(projectRoot)}\"\n  engine: Unity\n  unity_version: \"{Yaml(unity)}\"\ntechnology:\n  input_system: {input.ToString().ToLowerInvariant()}\nstructure:\n  scripts: {scripts}\n  scenes: {scenes}\n  prefabs: {prefabs}\n  test_directories: {tests}\nsource:\n  type: scanner\n  tool: project-scanner\n  observed_at_utc: \"{now}\"\n");

        File.WriteAllText(Path.Combine(dir, "state.yaml"), $"schema_version: 1\nproject:\n  status: analyzed\n  last_analyzed: \"{now}\"\nhealth:\n  critical: {health.Critical}\n  errors: {health.Errors}\n  warnings: {health.Warnings}\n  info: {health.Info}\nscan:\n  unity_version: \"{Yaml(unity)}\"\n  input_system: {input.ToString().ToLowerInvariant()}\n  scripts: {scripts}\n  scenes: {scenes}\nactive_work: []\n");

        var issues = new StringBuilder("schema_version: 1\nissues:\n");
        foreach (var issue in health.Issues)
        {
            issues.AppendLine($"  - id: {StableId(issue.Code, issue.Path)}");
            issues.AppendLine($"    code: {issue.Code}");
            issues.AppendLine($"    severity: {issue.Severity}");
            issues.AppendLine("    status: open");
            issues.AppendLine($"    message: \"{Yaml(issue.Message)}\"");
            if (!string.IsNullOrWhiteSpace(issue.Path)) issues.AppendLine($"    path: \"{Yaml(issue.Path!)}\"");
            issues.AppendLine($"    observed_at_utc: \"{now}\"");
            issues.AppendLine("    source: health-report");
        }
        File.WriteAllText(Path.Combine(dir, "issues.yaml"), issues.ToString());

        File.WriteAllText(Path.Combine(dir, "architecture.yaml"), $"schema_version: 1\nsystems: []\ndiscovered:\n  input:\n    detected: {input.ToString().ToLowerInvariant()}\n  rendering:\n    detected: false\n  physics:\n    detected: false\nsource:\n  type: scanner\n  tool: project-scanner\n  observed_at_utc: \"{now}\"\n");

        return dir;
    }

    private static string StableId(string code, string? path)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{code}|{path ?? string.Empty}".ToUpperInvariant()));
        return $"{code}-{Convert.ToHexString(bytes)[..8]}";
    }

    private static string Yaml(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
}
