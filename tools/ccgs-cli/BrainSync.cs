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

        File.WriteAllText(Path.Combine(dir, "project.yaml"), $"schema_version: 1\nproject:\n  name: \"{Yaml(Path.GetFileName(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))}\"\n  engine: Unity\n  unity_version: \"{Yaml(unity)}\"\ntechnology:\n  input_system: {input.ToString().ToLowerInvariant()}\nstructure:\n  scripts: {scripts}\n  scenes: {scenes}\n  prefabs: {prefabs}\n  test_directories: {tests}\nsource:\n  type: scanner\n  tool: project-scanner\n  observed_at_utc: \"{now}\"\n");

        File.WriteAllText(Path.Combine(dir, "state.yaml"), $"schema_version: 1\nproject:\n  status: analyzed\n  last_analyzed: \"{now}\"\nhealth:\n  critical: {health.Critical}\n  errors: {health.Errors}\n  warnings: {health.Warnings}\n  info: {health.Info}\nscan:\n  unity_version: \"{Yaml(unity)}\"\n  input_system: {input.ToString().ToLowerInvariant()}\n  scripts: {scripts}\n  scenes: {scenes}\nactive_work: []\n");

        SyncIssues(Path.Combine(dir, "issues.yaml"), health.Issues, now);

        File.WriteAllText(Path.Combine(dir, "architecture.yaml"), $"schema_version: 1\nsystems: []\ndiscovered:\n  input:\n    detected: {input.ToString().ToLowerInvariant()}\n  rendering:\n    detected: false\n  physics:\n    detected: false\nsource:\n  type: scanner\n  tool: project-scanner\n  observed_at_utc: \"{now}\"\n");

        return dir;
    }

    private static void SyncIssues(string file, IReadOnlyList<HealthIssue> detected, string now)
    {
        var existing = File.Exists(file) ? ReadIssues(File.ReadAllLines(file)) : new Dictionary<string, BrainIssue>();
        var current = detected.ToDictionary(x => StableId(x.Code, x.Path), StringComparer.OrdinalIgnoreCase);

        foreach (var issue in current)
        {
            if (!existing.TryGetValue(issue.Key, out var prior))
            {
                var legacyId = StableId(issue.Value.Code, null);
                if (!string.Equals(legacyId, issue.Key, StringComparison.OrdinalIgnoreCase) && existing.Remove(legacyId, out var legacy))
                {
                    legacy.Id = issue.Key;
                    prior = legacy;
                    existing[issue.Key] = prior;
                }
            }

            if (prior is not null)
            {
                var oldStatus = prior.Status;
                prior.Code = issue.Value.Code;
                prior.Severity = issue.Value.Severity;
                prior.Message = issue.Value.Message;
                prior.Path = issue.Value.Path;

                if (oldStatus is "resolved")
                {
                    prior.Status = "open";
                    prior.History.Add(new HistoryEntry("reopened", now, "health-report"));
                }
                else
                {
                    prior.Status = oldStatus is "ignored" ? "ignored" : "open";
                    if (!string.Equals(oldStatus, prior.Status, StringComparison.OrdinalIgnoreCase))
                        prior.History.Add(new HistoryEntry(prior.Status, now, "health-report"));
                }

                prior.ResolvedAtUtc = null;
                prior.LastObservedAtUtc = now;
            }
            else
            {
                existing[issue.Key] = new BrainIssue
                {
                    Id = issue.Key,
                    Code = issue.Value.Code,
                    Severity = issue.Value.Severity,
                    Status = "open",
                    Message = issue.Value.Message,
                    Path = issue.Value.Path,
                    LastObservedAtUtc = now,
                    Source = "health-report",
                    History = new List<HistoryEntry> { new("open", now, "health-report") }
                };
            }
        }

        foreach (var prior in existing.Values)
        {
            if (current.ContainsKey(prior.Id))
                continue;

            if (prior.Status is "open" or "in-progress")
            {
                prior.Status = "resolved";
                prior.History.Add(new HistoryEntry("resolved", now, "health-report"));
                prior.ResolvedAtUtc = now;
            }
        }

        WriteIssues(file, existing.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase));
    }

    private static Dictionary<string, BrainIssue> ReadIssues(string[] lines)
    {
        var result = new Dictionary<string, BrainIssue>(StringComparer.OrdinalIgnoreCase);
        BrainIssue? current = null;
        HistoryEntry? historyEntry = null;
        var inHistory = false;

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (line.StartsWith("  - id:", StringComparison.Ordinal))
            {
                if (current?.Id is not null)
                    result[current.Id] = current;
                current = new BrainIssue { Id = line[8..].Trim() };
                historyEntry = null;
                inHistory = false;
                continue;
            }

            if (current is null)
                continue;

            if (line.StartsWith("    history:", StringComparison.Ordinal))
            {
                inHistory = true;
                historyEntry = null;
                continue;
            }

            if (inHistory && line.StartsWith("      - ", StringComparison.Ordinal))
            {
                historyEntry = new HistoryEntry(string.Empty, string.Empty, string.Empty);
                current.History.Add(historyEntry);
                continue;
            }

            if (inHistory && line.StartsWith("        ", StringComparison.Ordinal) && historyEntry is not null)
            {
                var separator = line.IndexOf(':', 8);
                if (separator < 0)
                    continue;

                var key = line[8..separator];
                var value = Unquote(line[(separator + 1)..].Trim());
                historyEntry = historyEntry with
                {
                    Status = key == "status" ? value : historyEntry.Status,
                    AtUtc = key == "at_utc" ? value : historyEntry.AtUtc,
                    Source = key == "source" ? value : historyEntry.Source
                };
                current.History[^1] = historyEntry;
                continue;
            }

            if (line.StartsWith("    ", StringComparison.Ordinal))
            {
                if (inHistory)
                {
                    // A four-space field after history ends the history block.
                    inHistory = false;
                    historyEntry = null;
                }
            }

            if (!line.StartsWith("    ", StringComparison.Ordinal))
            {
                inHistory = false;
                historyEntry = null;
                continue;
            }

            var fieldSeparator = line.IndexOf(':', 4);
            if (fieldSeparator < 0)
                continue;

            var field = line[4..fieldSeparator];
            var rawValue = line[(fieldSeparator + 1)..].Trim();
            if (field == "history")
            {
                ReadLegacyHistory(rawValue, current);
                inHistory = false;
                continue;
            }

            var fieldValue = Unquote(rawValue);
            switch (field)
            {
                case "code": current.Code = fieldValue; break;
                case "severity": current.Severity = fieldValue; break;
                case "status": current.Status = fieldValue; break;
                case "message": current.Message = fieldValue; break;
                case "path": current.Path = fieldValue; break;
                case "observed_at_utc": current.LastObservedAtUtc = fieldValue; break;
                case "resolved_at_utc": current.ResolvedAtUtc = fieldValue; break;
                case "source": current.Source = fieldValue; break;
            }
        }

        if (current?.Id is not null)
            result[current.Id] = current;

        return result;
    }

    private static void ReadLegacyHistory(string rawValue, BrainIssue issue)
    {
        if (string.IsNullOrWhiteSpace(rawValue) || rawValue == "[]")
            return;

        var value = Unquote(rawValue);
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return;

            foreach (var item in document.RootElement.EnumerateArray())
            {
                var status = item.TryGetProperty("Status", out var statusValue) ? statusValue.GetString() ?? string.Empty : string.Empty;
                var atUtc = item.TryGetProperty("AtUtc", out var atUtcValue) ? atUtcValue.GetString() ?? string.Empty : string.Empty;
                var source = item.TryGetProperty("Source", out var sourceValue) ? sourceValue.GetString() ?? string.Empty : string.Empty;
                if (!string.IsNullOrWhiteSpace(status))
                    issue.History.Add(new HistoryEntry(status, atUtc, source));
            }
        }
        catch (JsonException)
        {
            // Keep the issue usable even if an older history value is malformed.
        }
    }

    private static void WriteIssues(string file, IEnumerable<BrainIssue> issues)
    {
        var builder = new StringBuilder("schema_version: 3\nissues:\n");
        foreach (var issue in issues)
        {
            builder.AppendLine($"  - id: {issue.Id}");
            builder.AppendLine($"    code: {issue.Code}");
            builder.AppendLine($"    severity: {issue.Severity}");
            builder.AppendLine($"    status: {issue.Status}");
            builder.AppendLine($"    message: \"{Yaml(issue.Message)}\"");
            if (!string.IsNullOrWhiteSpace(issue.Path))
                builder.AppendLine($"    path: \"{Yaml(issue.Path!)}\"");
            builder.AppendLine($"    observed_at_utc: \"{issue.LastObservedAtUtc}\"");
            if (!string.IsNullOrWhiteSpace(issue.ResolvedAtUtc))
                builder.AppendLine($"    resolved_at_utc: \"{issue.ResolvedAtUtc}\"");
            builder.AppendLine($"    source: {issue.Source}");
            builder.AppendLine("    history:");
            foreach (var entry in issue.History)
            {
                builder.AppendLine("      - status: " + entry.Status);
                builder.AppendLine("        at_utc: \"" + Yaml(entry.AtUtc) + "\"");
                builder.AppendLine("        source: " + entry.Source);
            }
        }
        File.WriteAllText(file, builder.ToString());
    }

    private static string StableId(string code, string? path)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{code}|{path ?? string.Empty}".ToUpperInvariant()));
        return $"{code}-{Convert.ToHexString(bytes)[..8]}";
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

    private sealed class BrainIssue
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = "open";
        public string Message { get; set; } = string.Empty;
        public string? Path { get; set; }
        public string LastObservedAtUtc { get; set; } = string.Empty;
        public string? ResolvedAtUtc { get; set; }
        public string Source { get; set; } = "health-report";
        public List<HistoryEntry> History { get; set; } = new();
    }

    private sealed record HistoryEntry(string Status, string AtUtc, string Source);
}
