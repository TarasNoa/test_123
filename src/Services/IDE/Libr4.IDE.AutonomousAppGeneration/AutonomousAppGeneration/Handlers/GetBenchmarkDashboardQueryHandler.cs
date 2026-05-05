using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using MediatR;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Handlers;

public sealed class GetBenchmarkDashboardQueryHandler
    : IRequestHandler<GetBenchmarkDashboardQuery, BenchmarkDashboardDto>
{
    private readonly IAppGenerationRepository _repository;
    private readonly IRunQualityAssessmentService _qualityAssessment;

    public GetBenchmarkDashboardQueryHandler(
        IAppGenerationRepository repository,
        IRunQualityAssessmentService qualityAssessment)
    {
        _repository = repository;
        _qualityAssessment = qualityAssessment;
    }

    public async Task<BenchmarkDashboardDto> Handle(GetBenchmarkDashboardQuery request, CancellationToken ct)
    {
        var limit = Math.Clamp(request.Limit, 1, 200);
        var runs = await _repository.ListAsync(ct);
        var selected = runs
            .OrderByDescending(r => r.UpdatedAt)
            .Take(limit)
            .ToList();

        var runDtos = selected.Select(r =>
        {
            var quality = _qualityAssessment.Assess(r);
            var duration = r.Iterations
                .Where(i => i.Execution is not null)
                .SelectMany(i => i.Execution!.CommandExecutions)
                .Sum(c => (long)c.Duration.TotalMilliseconds);
            return new BenchmarkRunPointDto(
                RunId: r.Id,
                Status: r.Status.ToString(),
                StartedAtUtc: r.StartedAt,
                CompletedAtUtc: r.CompletedAt,
                OverallScore: quality.OverallScore,
                FailedQualityGates: r.QualityGates.Count(g => !g.Passed),
                TotalCommandDurationMs: duration);
        }).ToList();

        var topFailureReasons = selected
            .SelectMany(r => r.QualityGates.Where(g => !g.Passed))
            .SelectMany(g => g.Reasons)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .GroupBy(r => r, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(g => g.Key)
            .ToList();

        static bool IsMcpDegraded(string outcome) =>
            outcome.Equals("mcp_server_missing", StringComparison.OrdinalIgnoreCase)
            || outcome.Equals("mcp_server_unreachable", StringComparison.OrdinalIgnoreCase)
            || outcome.Equals("mcp_server_unavailable", StringComparison.OrdinalIgnoreCase);

        var mcpDegradedEvents = selected
            .SelectMany(r => r.McpExecutions)
            .Where(m => IsMcpDegraded(m.Outcome))
            .ToList();

        var topMcpBlockers = mcpDegradedEvents
            .GroupBy(x => x.Outcome, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(g => g.Key)
            .ToList();

        var stageDurationAverages = selected
            .SelectMany(r => r.Iterations.Where(i => i.Execution is not null)
                .SelectMany(i => i.Execution!.CommandExecutions))
            .GroupBy(c => NormalizeStage(c.Phase), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Count() > 0 ? (long)Math.Round(g.Average(x => x.Duration.TotalMilliseconds)) : 0,
                StringComparer.OrdinalIgnoreCase);

        var stageTrends = selected
            .SelectMany(r => r.QualityGates)
            .GroupBy(g => NormalizeStage(g.Stage), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var evals = g.Count();
                var passed = g.Count(x => x.Passed);
                var passRate = evals == 0 ? 0d : Math.Round((double)passed / evals, 4);
                var avgScore = evals == 0 ? 0d : Math.Round(g.Average(x => x.Score), 2);
                var avgDuration = stageDurationAverages.TryGetValue(g.Key, out var d) ? d : 0;
                return new BenchmarkStageTrendDto(
                    Stage: g.Key,
                    Evaluations: evals,
                    AverageScore: avgScore,
                    PassRate: passRate,
                    AverageDurationMs: avgDuration);
            })
            .OrderByDescending(s => s.Evaluations)
            .ThenBy(s => s.Stage, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var topRegressions = BuildTopRegressions(selected);

        var failed = selected.Count(r => string.Equals(r.Status.ToString(), "Failed", StringComparison.OrdinalIgnoreCase));
        var succeeded = selected.Count(r => string.Equals(r.Status.ToString(), "Completed", StringComparison.OrdinalIgnoreCase));
        var successRate = selected.Count == 0
            ? 0d
            : Math.Round((double)succeeded / selected.Count, 4);

        return new BenchmarkDashboardDto(
            GeneratedAtUtc: DateTime.UtcNow,
            TotalRuns: selected.Count,
            SucceededRuns: succeeded,
            FailedRuns: failed,
            SuccessRate: successRate,
            TotalMcpDegradedEvents: mcpDegradedEvents.Count,
            TopMcpBlockerCodes: topMcpBlockers,
            TopFailureReasons: topFailureReasons,
            StageTrends: stageTrends,
            TopRegressions: topRegressions,
            Runs: runDtos);
    }

    private static string NormalizeStage(string stage)
    {
        if (string.IsNullOrWhiteSpace(stage)) return "unknown";
        var idx = stage.IndexOf(':');
        return idx > 0 ? stage[..idx] : stage;
    }

    private static IReadOnlyList<BenchmarkRegressionDto> BuildTopRegressions(IReadOnlyList<Domain.AutonomousAppGeneration.AppGenerationOrchestrator> selected)
    {
        if (selected.Count < 2)
            return Array.Empty<BenchmarkRegressionDto>();

        var latestRun = selected
            .OrderByDescending(r => r.UpdatedAt)
            .First();

        var baselineRuns = selected
            .Where(r => r.Id != latestRun.Id)
            .ToList();

        var latestByStage = latestRun.QualityGates
            .GroupBy(g => NormalizeStage(g.Stage), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.EvaluatedAtUtc).First(),
                StringComparer.OrdinalIgnoreCase);

        var baselineAverages = baselineRuns
            .SelectMany(r => r.QualityGates)
            .GroupBy(g => NormalizeStage(g.Stage), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Average(x => x.Score),
                StringComparer.OrdinalIgnoreCase);

        var regressions = new List<BenchmarkRegressionDto>();
        foreach (var (stage, latest) in latestByStage)
        {
            if (!baselineAverages.TryGetValue(stage, out var baselineAvg))
                continue;

            var delta = Math.Round(latest.Score - baselineAvg, 2);
            if (delta >= 0)
                continue;

            regressions.Add(new BenchmarkRegressionDto(
                Stage: stage,
                BaselineAverageScore: Math.Round(baselineAvg, 2),
                LatestScore: latest.Score,
                Delta: delta,
                LatestFailureReasons: latest.Reasons.ToList()));
        }

        return regressions
            .OrderBy(r => r.Delta)
            .ThenBy(r => r.Stage, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }
}
