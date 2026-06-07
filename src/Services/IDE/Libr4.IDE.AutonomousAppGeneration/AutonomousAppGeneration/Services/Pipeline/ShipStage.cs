using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

public sealed class ShipStage : IGenerationStage
{
    private readonly IGitHubShipService _githubShip;
    private readonly GitHubActionsDispatchOptions _githubOptions;
    private readonly IReviewGate? _reviewGate;
    private readonly IObscuraEvidenceShipGate? _evidenceGate;
    private readonly IFleetShipSyncService? _fleetShipSync;
    private readonly ILogger<ShipStage> _logger;

    public ShipStage(
        IGitHubShipService githubShip,
        IOptions<GitHubActionsDispatchOptions> githubOptions,
        ILogger<ShipStage> logger,
        IReviewGate? reviewGate = null,
        IObscuraEvidenceShipGate? evidenceGate = null,
        IFleetShipSyncService? fleetShipSync = null)
    {
        _githubShip = githubShip;
        _githubOptions = githubOptions.Value;
        _reviewGate = reviewGate;
        _evidenceGate = evidenceGate;
        _fleetShipSync = fleetShipSync;
        _logger = logger;
    }

    public string Name => "ship";
    public int Order => 500;

    public async Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        context.Items["ship_stage_reached"] = true;

        if (_reviewGate is not null && _reviewGate.RequireHumanReview)
        {
            var approved = await _reviewGate.IsApprovedAsync(context.Orchestrator.Id, ct).ConfigureAwait(false);
            context.Items["human_review_required"] = true;
            context.Items["human_review_approved"] = approved;

            if (!approved)
            {
                var status = await _reviewGate.GetStatusAsync(context.Orchestrator.Id, ct).ConfigureAwait(false);
                context.Items["human_review_status"] = status.ToString();
                _logger.LogInformation(
                    "[ShipStage] Blocked run={RunId} pending human review status={Status}",
                    context.Orchestrator.Id,
                    status);
                return StageOutcome.Stop("human_review_pending");
            }
        }

        if (_evidenceGate is not null)
        {
            var gateResult = await _evidenceGate.EvaluateAsync(context, ct).ConfigureAwait(false);
            context.Items["ship_evidence_gate_passed"] = gateResult.Allowed;
            context.Items["ship_obscura_artifact_count"] = gateResult.ObscuraArtifactCount;

            if (!gateResult.Allowed)
            {
                context.Items["ship_evidence_block_reason"] = gateResult.BlockReason ?? "blocked";
                _logger.LogWarning(
                    "[ShipStage] Blocked run={RunId} evidence gate reason={Reason}",
                    context.Orchestrator.Id,
                    gateResult.BlockReason);
                return StageOutcome.Stop(gateResult.BlockReason ?? "ship_evidence_blocked");
            }
        }

        var shipResult = await _githubShip.ShipAsync(context, ct).ConfigureAwait(false);
        context.Items["github_ship_result"] = shipResult;
        context.Items["github_ship_skipped"] = shipResult.Skipped;
        context.Items["github_ship_success"] = shipResult.Success;

        if (shipResult.PullRequestNumber is not null)
            context.Items["github_pull_request_number"] = shipResult.PullRequestNumber;
        if (!string.IsNullOrWhiteSpace(shipResult.PullRequestUrl))
            context.Items["github_pull_request_url"] = shipResult.PullRequestUrl;
        if (!string.IsNullOrWhiteSpace(shipResult.HeadBranch))
            context.Items["github_head_branch"] = shipResult.HeadBranch;

        if (!shipResult.Skipped)
        {
            context.Orchestrator.RecordQualityGate(
                "github_ship",
                shipResult.Success ? 5 : 1,
                shipResult.Success,
                new[] { shipResult.Summary });

            _logger.LogInformation(
                "[ShipStage] GitHub ship run={RunId} success={Success} skipped={Skipped} summary={Summary}",
                context.Orchestrator.Id,
                shipResult.Success,
                shipResult.Skipped,
                shipResult.Summary);
        }

        if (!shipResult.Skipped && !shipResult.Success && _githubOptions.RequireShipSuccess)
            return StageOutcome.Stop("github_ship_failed");

        if (_fleetShipSync is not null && shipResult.Success && !shipResult.Skipped)
        {
            await _fleetShipSync.RecordShipResultAsync(context.Orchestrator.Id, shipResult, ct)
                .ConfigureAwait(false);
        }

        return PipelineStageHelper.MarkAndContinue(
            context.Orchestrator,
            AutonomousPipelineStages.Completed);
    }
}
