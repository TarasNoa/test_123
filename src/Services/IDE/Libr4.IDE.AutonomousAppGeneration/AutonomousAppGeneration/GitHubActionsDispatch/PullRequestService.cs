using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;

public interface IPullRequestService
{
    Task<GitHubShipResult> CreatePrAsync(Guid runId, CancellationToken ct = default);
}

public sealed class PullRequestService : IPullRequestService
{
    private readonly IAppGenerationRepository _repository;
    private readonly IGitHubShipService _ship;
    private readonly IFleetShipSyncService _fleetSync;
    private readonly IReviewGate? _reviewGate;
    private readonly ILogger<PullRequestService> _logger;

    public PullRequestService(
        IAppGenerationRepository repository,
        IGitHubShipService ship,
        IFleetShipSyncService fleetSync,
        ILogger<PullRequestService> logger,
        IReviewGate? reviewGate = null)
    {
        _repository = repository;
        _ship = ship;
        _fleetSync = fleetSync;
        _reviewGate = reviewGate;
        _logger = logger;
    }

    public async Task<GitHubShipResult> CreatePrAsync(Guid runId, CancellationToken ct = default)
    {
        var orchestrator = await _repository.GetAsync(runId, ct).ConfigureAwait(false);
        if (orchestrator is null)
            return GitHubShipResult.Failed("run_not_found");

        if (_reviewGate is not null && _reviewGate.RequireHumanReview)
        {
            var approved = await _reviewGate.IsApprovedAsync(runId, ct).ConfigureAwait(false);
            if (!approved)
                return GitHubShipResult.Failed("human_review_pending");
        }

        var context = BuildContext(orchestrator);
        var result = await _ship.ShipAsync(context, ct).ConfigureAwait(false);
        if (result.Success && !result.Skipped)
            await _fleetSync.RecordShipResultAsync(runId, result, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "CreatePrAsync run={RunId} success={Success} skipped={Skipped}",
            runId,
            result.Success,
            result.Skipped);

        return result;
    }

    private static GenerationContext BuildContext(AppGenerationOrchestrator orchestrator)
    {
        var context = new GenerationContext
        {
            Orchestrator = orchestrator,
            UserRequest = orchestrator.UserRequest ?? string.Empty,
            Plan = orchestrator.Plan
        };
        context.Files.AddRange(orchestrator.Files);

        var verifyGate = orchestrator.QualityGates
            .LastOrDefault(g => g.Stage.Equals("verify_subagent", StringComparison.OrdinalIgnoreCase));
        if (verifyGate is not null)
            context.Items["verify_passed"] = verifyGate.Passed;

        return context;
    }
}
