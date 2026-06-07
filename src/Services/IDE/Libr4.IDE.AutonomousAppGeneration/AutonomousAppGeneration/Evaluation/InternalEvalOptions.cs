namespace Libr4.IDE.Application.AutonomousAppGeneration.Evaluation;

public sealed class InternalEvalOptions
{
    public const string SectionName = "AutonomousAppGeneration:InternalEval";

    public bool Enabled { get; set; } = true;

    /// <summary>Root of Evaluation/ suite (benchmarks + baselines).</summary>
    public string EvaluationRoot { get; set; } = "Evaluation";

    public string BaselineScoresPath { get; set; } = "Evaluation/baselines/scores.json";
}

public static class EvalStackNames
{
    public const string DjangoViews = "django-views";
    public const string ReactComponents = "react-components";
    public const string DotNetControllers = "dotnet-controllers";
    public const string MbppAlgorithms = "mbpp-algorithms";

    public static readonly IReadOnlyList<string> All =
    [
        DjangoViews,
        ReactComponents,
        DotNetControllers,
        MbppAlgorithms
    ];
}

public static class EvalBenchmarkStyles
{
    public const string HumanEval = "humaneval";
    public const string Mbpp = "mbpp";
}
