using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public interface IRunQualityAssessmentService
{
    RunQualityAssessmentDto Assess(AppGenerationOrchestrator orchestrator);
}

public sealed class RunQualityAssessmentService : IRunQualityAssessmentService
{
    public RunQualityAssessmentDto Assess(AppGenerationOrchestrator orchestrator)
    {
        var groups = orchestrator.QualityGates
            .GroupBy(g => NormalizeStage(g.Stage), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .ToList();

        var stageScores = groups.Select(g =>
        {
            var latest = g.OrderBy(x => x.EvaluatedAtUtc).Last();
            return new StageQualityScoreDto(
                Stage: g.Key,
                LatestScore: latest.Score,
                AverageScore: Math.Round(g.Average(x => x.Score), 2),
                Evaluations: g.Count(),
                LastPassed: latest.Passed);
        }).ToList();

        var weighted = stageScores.Count == 0
            ? 0
            : (int)Math.Round(stageScores.Average(x => x.LatestScore), MidpointRounding.AwayFromZero);

        var verdict = weighted switch
        {
            >= 9 => "excellent",
            >= 8 => "good",
            >= 7 => "acceptable",
            >= 5 => "needs_improvement",
            _ => "critical"
        };

        return new RunQualityAssessmentDto(
            OverallScore: Math.Clamp(weighted, 0, 10),
            Verdict: verdict,
            StageScores: stageScores);
    }

    private static string NormalizeStage(string stage)
    {
        if (string.IsNullOrWhiteSpace(stage)) return "unknown";
        var idx = stage.IndexOf(':');
        return idx > 0 ? stage[..idx] : stage;
    }
}
