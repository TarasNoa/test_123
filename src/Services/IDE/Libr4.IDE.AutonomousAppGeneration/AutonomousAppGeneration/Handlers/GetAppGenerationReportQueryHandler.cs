using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using MediatR;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Handlers;

public sealed class GetAppGenerationReportQueryHandler
    : IRequestHandler<GetAppGenerationReportQuery, AppGenerationReportDto?>
{
    private readonly IAppGenerationRepository _repository;
    private readonly IExecutionManifestBuilder _manifestBuilder;
    private readonly IRunQualityAssessmentService _qualityAssessment;
    private readonly ITaskGraphHydrationService _taskGraphHydration;

    public GetAppGenerationReportQueryHandler(
        IAppGenerationRepository repository,
        IExecutionManifestBuilder manifestBuilder,
        IRunQualityAssessmentService qualityAssessment,
        ITaskGraphHydrationService taskGraphHydration)
    {
        _repository = repository;
        _manifestBuilder = manifestBuilder;
        _qualityAssessment = qualityAssessment;
        _taskGraphHydration = taskGraphHydration;
    }

    public async Task<AppGenerationReportDto?> Handle(
        GetAppGenerationReportQuery request, CancellationToken ct)
    {
        var o = await _repository.GetAsync(request.OrchestratorId, ct);
        if (o is null) return null;

        _taskGraphHydration.EnsureHydrated(o);

        var iterations = o.Iterations.Select(i => new IterationDto(
            Id: i.Id,
            Number: i.Number,
            Succeeded: i.Succeeded,
            ErrorCount: i.Errors.Count,
            AppliedFixes: i.AppliedFixes.ToList(),
            RetryCount: i.RetryEvents.Count,
            RetryEvents: i.RetryEvents.Select(r => new RetryEventDto(
                Attempt: r.Attempt,
                Reason: r.Reason,
                BackoffMs: r.BackoffMs,
                TimestampUtc: r.TimestampUtc)).ToList(),
            StartedAt: i.StartedAt,
            CompletedAt: i.CompletedAt)).ToList();

        var files = o.Files.Select(f => new GeneratedFileDto(
            f.RelativePath, f.Language, f.Content, f.UpdatedAt)).ToList();

        var outstanding = o.Iterations.LastOrDefault()?.Errors ?? (IReadOnlyList<ErrorReport>)Array.Empty<ErrorReport>();
        var outstandingDtos = outstanding.Select(e => new ErrorReportDto(
            e.ErrorType, e.Message, e.FilePath, e.LineNumber, e.SuggestedFix, e.DiagnosingAgent)).ToList();
        var qualityGates = o.QualityGates.Select(g => new QualityGateResultDto(
            Stage: g.Stage,
            Score: g.Score,
            Passed: g.Passed,
            Reasons: g.Reasons.ToList(),
            EvaluatedAtUtc: g.EvaluatedAtUtc)).ToList();
        var qualityAssessment = _qualityAssessment.Assess(o);

        var manifest = await _manifestBuilder.BuildAndPersistAsync(o, ct);
        var cascadePlan = o.CascadePlans
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CascadePlanTraceDto(
                Rationale: c.Rationale,
                SerializedPlanJson: c.SerializedPlanJson,
                PhaseCount: c.PhaseCount,
                RoutingProfile: c.RoutingProfile,
                ModelHint: c.ModelHint,
                PlannerMode: c.PlannerMode,
                CreatedAtUtc: c.CreatedAtUtc))
            .FirstOrDefault();

        GenerationPlanDto? planDto = null;
        if (o.Plan is not null)
        {
            planDto = new GenerationPlanDto(
                ApplicationName: o.Plan.ApplicationName,
                ApplicationDescription: o.Plan.ApplicationDescription,
                TechStack: new TechStackDto(
                    Languages: o.Plan.TechStack.Languages.ToList(),
                    Frameworks: o.Plan.TechStack.Frameworks.ToList(),
                    Databases: o.Plan.TechStack.Databases.ToList(),
                    Infrastructure: o.Plan.TechStack.Infrastructure.ToList(),
                    Rationale: o.Plan.TechStack.Rationale),
                Phases: o.Plan.Phases.Select(p => new GenerationPhaseDto(
                    Order: p.Order,
                    Name: p.Name,
                    Description: p.Description,
                    Assignments: p.Assignments.Select(a =>
                        new AgentAssignmentDto(a.AgentName, a.Role, a.TaskDescription)).ToList())).ToList(),
                RequiredAgents: o.Plan.RequiredAgents.ToList(),
                RuntimeImage: o.Plan.RuntimeImage,
                BuildCommands: o.Plan.BuildCommands.ToList(),
                TestCommands: o.Plan.TestCommands.ToList(),
                MaxIterations: o.Plan.MaxIterations);
        }

        var memoryRetrievalDtos = o.MemoryRetrievals
            .Select(r => new MemoryRetrievalDto(
                RunId: r.RunId,
                Stage: r.Stage,
                Kind: r.Kind.ToString(),
                Key: r.Key,
                Summary: r.Summary,
                RetrievalReason: r.RetrievalReason,
                RelevanceScore: r.RelevanceScore,
                RetrievedAtUtc: r.RetrievedAtUtc))
            .ToList();

        return new AppGenerationReportDto(
            Id: o.Id,
            Status: o.Status.ToString(),
            FailureReason: o.FailureReason,
            ApplicationName: o.Plan?.ApplicationName,
            FileCount: o.Files.Count,
            Plan: planDto,
            QualityGates: qualityGates,
            QualityAssessment: qualityAssessment,
            Iterations: iterations,
            Files: files,
            OutstandingErrors: outstandingDtos,
            Manifest: manifest,
            BenchmarkSummary: manifest.BenchmarkSummary,
            MemoryRetrievals: memoryRetrievalDtos,
            CascadePlan: cascadePlan,
            StartedAt: o.StartedAt,
            CompletedAt: o.CompletedAt);
    }
}
