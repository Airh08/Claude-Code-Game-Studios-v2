namespace Ccgs.Cli;

public sealed record RoutingDecision(
    string? PrimaryAgent,
    IReadOnlyList<string> SupportingAgents,
    string MatchedRule,
    string Rationale);

public static class TaskRouter
{
    public static RoutingDecision RouteIssueCode(string issueCode, AgentCapabilityRegistry? registry = null)
    {
        registry ??= AgentCapabilities.Load();
        var rule = registry.IssueCodeRules.FirstOrDefault(r => string.Equals(r.Code, issueCode, StringComparison.OrdinalIgnoreCase));
        if (rule is null)
        {
            return new RoutingDecision(
                null,
                Array.Empty<string>(),
                "unmatched",
                $"Issue code '{issueCode}' does not have a deterministic routing rule yet.");
        }

        return new RoutingDecision(rule.Primary, rule.Supporting, $"issue-code:{rule.Code}", rule.Rationale);
    }

    public static RoutingDecision RouteTask(BrainTask task, AgentCapabilityRegistry? registry = null)
    {
        if (!string.IsNullOrWhiteSpace(task.AssignedAgent))
        {
            return new RoutingDecision(
                task.AssignedAgent,
                Array.Empty<string>(),
                "explicit-assignment",
                $"Task explicitly assigned to '{task.AssignedAgent}' by the requester.");
        }

        registry ??= AgentCapabilities.Load();
        var type = task.Type.Trim().ToLowerInvariant();
        var rule = registry.TaskTypeRules.FirstOrDefault(r => string.Equals(r.Type, type, StringComparison.OrdinalIgnoreCase));
        if (rule is null)
        {
            return new RoutingDecision(
                null,
                Array.Empty<string>(),
                "unmatched",
                $"Task type '{task.Type}' does not map to a single domain. Assign an agent explicitly (--agent) or route by the affected Brain issue code.");
        }

        return new RoutingDecision(rule.Primary, rule.Supporting, $"task-type:{rule.Type}", rule.Rationale);
    }
}
