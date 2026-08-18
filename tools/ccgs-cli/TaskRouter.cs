namespace Ccgs.Cli;

public sealed record RoutingDecision(
    string? PrimaryAgent,
    IReadOnlyList<string> SupportingAgents,
    string MatchedRule,
    string Rationale);

public static class TaskRouter
{
    public static RoutingDecision RouteIssueCode(string issueCode) => issueCode switch
    {
        "BUILD-001" => new RoutingDecision(
            "unity-engineer",
            new[] { "qa-engineer" },
            "issue-code:BUILD-001",
            "Build Settings references an invalid scene path. Unity Engineer owns Build Settings; QA validates the fix."),
        "INPUT-001" => new RoutingDecision(
            "unity-engineer",
            new[] { "gameplay-programmer" },
            "issue-code:INPUT-001",
            "Input System callback configuration requires Unity Editor verification. Unity Engineer leads; Gameplay Programmer supports the affected interaction code."),
        "TEST-001" => new RoutingDecision(
            "qa-engineer",
            new[] { "unity-engineer" },
            "issue-code:TEST-001",
            "No automated test coverage was discovered. QA owns establishing a baseline; Unity Engineer supports test infrastructure."),
        "PROJECT-001" => new RoutingDecision(
            "unity-engineer",
            Array.Empty<string>(),
            "issue-code:PROJECT-001",
            "The directory is not recognized as a Unity project. Unity Engineer must verify the project root."),
        _ => new RoutingDecision(
            null,
            Array.Empty<string>(),
            "unmatched",
            $"Issue code '{issueCode}' does not have a deterministic routing rule yet.")
    };

    public static RoutingDecision RouteTask(BrainTask task)
    {
        if (!string.IsNullOrWhiteSpace(task.AssignedAgent))
        {
            return new RoutingDecision(
                task.AssignedAgent,
                Array.Empty<string>(),
                "explicit-assignment",
                $"Task explicitly assigned to '{task.AssignedAgent}' by the requester.");
        }

        return task.Type.Trim().ToLowerInvariant() switch
        {
            "ui" => new RoutingDecision(
                "ui-engineer",
                new[] { "gameplay-programmer" },
                "task-type:ui",
                "UI implementation tasks are owned by the UI Engineer, with Gameplay Programmer support for HUD/gameplay integration."),
            "technical-art" => new RoutingDecision(
                "technical-artist",
                new[] { "unity-engineer" },
                "task-type:technical-art",
                "Technical art / asset pipeline tasks are owned by the Technical Artist, with Unity Engineer support for engine integration."),
            "testing" => new RoutingDecision(
                "qa-engineer",
                new[] { "unity-engineer" },
                "task-type:testing",
                "Testing/regression tasks are owned by QA, with Unity Engineer support for environment or tooling issues."),
            "design" => new RoutingDecision(
                "game-designer",
                new[] { "gameplay-programmer" },
                "task-type:design",
                "Game rules/design tasks are owned by the Game Designer, with Gameplay Programmer support for feasibility."),
            "content" => new RoutingDecision(
                "game-designer",
                Array.Empty<string>(),
                "task-type:content",
                "Content tasks default to the Game Designer in the absence of a more specific domain signal."),
            "analysis" => new RoutingDecision(
                null,
                Array.Empty<string>(),
                "task-type:analysis",
                "Analysis tasks do not require a specialist agent unless a concrete fix is requested."),
            _ => new RoutingDecision(
                null,
                Array.Empty<string>(),
                "unmatched",
                $"Task type '{task.Type}' does not map to a single domain. Assign an agent explicitly (--agent) or route by the affected Brain issue code.")
        };
    }
}
