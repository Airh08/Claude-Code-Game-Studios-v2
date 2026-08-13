using System.Text.Json;

namespace Ccgs.Cli;

public sealed record HealthIssue(
    string Code,
    string Severity,
    string Message,
    string? Path = null);

public sealed class HealthReport
{
    public int Critical { get; init; }
    public int Errors { get; init; }
    public int Warnings { get; init; }
    public int Info { get; init; }
    public List<HealthIssue> Issues { get; init; } = new();

    public static HealthReport FromScan(JsonElement scan)
    {
        var issues = new List<HealthIssue>();

        if (!scan.GetProperty("IsUnityProject").GetBoolean())
        {
            issues.Add(new HealthIssue(
                "PROJECT-001",
                "critical",
                "The selected directory is not recognized as a Unity project."));
        }

        // Missing path and missing GUID entries describe the same Build Settings
        // records in the current scanner. Keep the health report actionable and
        // avoid counting the same broken entry twice. The raw GUID list remains
        // available under Scanner for deeper diagnostics.
        foreach (var path in scan.GetProperty("MissingBuildScenes").EnumerateArray())
        {
            issues.Add(new HealthIssue(
                "BUILD-001",
                "error",
                "Build Settings references a scene path that does not exist.",
                path.GetString()));
        }

        foreach (var warning in scan.GetProperty("InputCallbackWarnings").EnumerateArray())
        {
            var message = warning.GetString() ?? "Input callback configuration requires Unity Editor inspection.";
            issues.Add(new HealthIssue(
                "INPUT-001",
                "warning",
                message,
                ExtractSubjectPath(message)));
        }

        if (scan.GetProperty("TestDirectories").GetArrayLength() == 0)
        {
            issues.Add(new HealthIssue(
                "TEST-001",
                "warning",
                "No test directories were discovered; establish a baseline of automated tests."));
        }

        foreach (var warning in scan.GetProperty("Warnings").EnumerateArray())
        {
            var message = warning.GetString() ?? "Scanner warning.";
            if (message.Contains("Build Settings scene path(s)", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Build Settings scene GUID(s)", StringComparison.OrdinalIgnoreCase))
                continue;

            issues.Add(new HealthIssue("SCAN-001", "warning", message));
        }

        return new HealthReport
        {
            Critical = issues.Count(x => x.Severity == "critical"),
            Errors = issues.Count(x => x.Severity == "error"),
            Warnings = issues.Count(x => x.Severity == "warning"),
            Info = issues.Count(x => x.Severity == "info"),
            Issues = issues
        };
    }

    private static string? ExtractSubjectPath(string message)
    {
        var separator = message.IndexOf(':');
        if (separator <= 0)
            return null;

        var candidate = message[..separator].Trim();
        return candidate.Contains('/') && candidate.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            ? candidate
            : null;
    }
}
