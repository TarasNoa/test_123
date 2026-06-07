namespace Libr4.IDE.Application.AutonomousAppGeneration.Evaluation;

public static class HumanEvalStyleEvaluator
{
    public static (bool Passed, List<string> Failures) Evaluate(
        EvalBenchmarkDefinition benchmark,
        string candidate)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            failures.Add("empty_candidate");
            return (false, failures);
        }

        foreach (var pattern in benchmark.RequiredPatterns)
        {
            if (!ContainsPattern(candidate, pattern))
                failures.Add($"missing_required:{pattern}");
        }

        foreach (var pattern in benchmark.ForbiddenPatterns)
        {
            if (ContainsPattern(candidate, pattern))
                failures.Add($"forbidden:{pattern}");
        }

        return (failures.Count == 0, failures);
    }

    internal static bool ContainsPattern(string candidate, string pattern) =>
        candidate.Contains(pattern, StringComparison.OrdinalIgnoreCase);
}

public static class MbppStyleEvaluator
{
    public static (bool Passed, List<string> Failures) Evaluate(
        EvalBenchmarkDefinition benchmark,
        string candidate)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            failures.Add("empty_candidate");
            return (false, failures);
        }

        var humaneval = HumanEvalStyleEvaluator.Evaluate(benchmark, candidate);
        failures.AddRange(humaneval.Failures);

        foreach (var test in benchmark.Tests)
        {
            foreach (var pattern in test.RequiredPatterns)
            {
                if (!HumanEvalStyleEvaluator.ContainsPattern(candidate, pattern))
                    failures.Add($"mbpp:{test.Name}:missing:{pattern}");
            }

            if (test.AnyOf.Count > 0
                && !test.AnyOf.Any(pattern => HumanEvalStyleEvaluator.ContainsPattern(candidate, pattern)))
            {
                failures.Add($"mbpp:{test.Name}:missing_any_of:{string.Join('|', test.AnyOf)}");
            }
        }

        return (failures.Count == 0, failures);
    }
}
