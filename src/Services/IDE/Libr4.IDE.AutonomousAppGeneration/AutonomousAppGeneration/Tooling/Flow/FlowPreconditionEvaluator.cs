namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;

public static class FlowPreconditionEvaluator
{
    public static (bool Passed, string? Reason) EvaluateAll(
        IReadOnlyList<FlowPrecondition> preconditions,
        FlowRuntimeContext context)
    {
        foreach (var precondition in preconditions)
        {
            var (passed, reason) = Evaluate(precondition, context);
            if (!passed)
                return (false, reason);
        }

        return (true, null);
    }

    public static (bool Passed, string? Reason) Evaluate(FlowPrecondition precondition, FlowRuntimeContext context) =>
        precondition.Kind.ToLowerInvariant() switch
        {
            "files_exist" => EvaluateFilesExist(precondition.Paths, context.WorkspaceFiles),
            "tests_pass" => context.TestsPassed ? (true, null) : (false, "tests_pass precondition failed"),
            "verify_passed" => context.VerifyPassed ? (true, null) : (false, "verify_passed precondition failed"),
            _ => (true, null)
        };

    private static (bool Passed, string? Reason) EvaluateFilesExist(
        IReadOnlyList<string> requiredPaths,
        IReadOnlyList<string> workspaceFiles)
    {
        if (requiredPaths.Count == 0)
            return (true, null);

        var present = workspaceFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = requiredPaths.Where(p => !present.Contains(Normalize(p))).ToList();
        return missing.Count == 0
            ? (true, null)
            : (false, $"missing files: {string.Join(", ", missing)}");
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/').TrimStart('/');
}
