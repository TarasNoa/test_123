using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using MediatR;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Handlers;

public sealed class GetBuildDiagnosticsDashboardQueryHandler
    : IRequestHandler<GetBuildDiagnosticsDashboardQuery, BuildDiagnosticsDashboardDto?>
{
    private readonly IAppGenerationRepository _repository;
    private readonly IRunQualityAssessmentService _qualityAssessment;
    private readonly IBuildDiagnosticsDashboardService _dashboard;
    private readonly IVerifyRecipeRegistry _verifyRecipes;
    private readonly VerifySubagentOptions _verifyOptions;

    public GetBuildDiagnosticsDashboardQueryHandler(
        IAppGenerationRepository repository,
        IRunQualityAssessmentService qualityAssessment,
        IBuildDiagnosticsDashboardService dashboard,
        IVerifyRecipeRegistry verifyRecipes,
        IOptions<VerifySubagentOptions> verifyOptions)
    {
        _repository = repository;
        _qualityAssessment = qualityAssessment;
        _dashboard = dashboard;
        _verifyRecipes = verifyRecipes;
        _verifyOptions = verifyOptions.Value;
    }

    public async Task<BuildDiagnosticsDashboardDto?> Handle(
        GetBuildDiagnosticsDashboardQuery request,
        CancellationToken ct)
    {
        var orchestrator = await _repository.GetAsync(request.OrchestratorId, ct).ConfigureAwait(false);
        if (orchestrator is null)
            return null;

        var verifyRecipe = await _verifyRecipes.DetectAsync(
            new VerifyRecipeDetectionRequest(
                orchestrator.Files,
                orchestrator.Plan,
                orchestrator.UserRequest,
                orchestrator.Id,
                _verifyOptions.EvidenceRoot),
            ct).ConfigureAwait(false);

        var quality = _qualityAssessment.Assess(orchestrator);
        return _dashboard.Build(orchestrator, quality, verifyRecipe, request.StackFilter);
    }
}
