using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

/// <summary>
/// P1-3 stage: looks up an existing run by request fingerprint. If a non-failed
/// matching run exists, the pipeline short-circuits with that orchestrator —
/// preventing duplicate work for repeated requests. Mirrors the inline logic in
/// <c>StartAppGenerationCommandHandler.Handle</c> (lines ~200-216).
///
/// Order=10 — must run before plan generation (Order=100).
/// </summary>
public sealed class IdempotencyCheckStage : IGenerationStage
{
    private readonly IAppGenerationRepository _repository;
    private readonly ILogger<IdempotencyCheckStage> _logger;

    public IdempotencyCheckStage(
        IAppGenerationRepository repository,
        ILogger<IdempotencyCheckStage> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public string Name => "idempotency_check";
    public int Order => 10;

    public async Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(context.Fingerprint))
        {
            // No fingerprint provided → cannot dedupe; continue.
            return StageOutcome.Continue;
        }

        var existing = await _repository.FindLatestByFingerprintAsync(context.Fingerprint, ct).ConfigureAwait(false);
        if (existing is null) return StageOutcome.Continue;

        // Skip the current run itself — the handler saves it before calling the pipeline.
        if (existing.Id == context.Orchestrator.Id) return StageOutcome.Continue;

        // Only reuse genuinely completed runs; failed / in-progress / planning runs get re-executed.
        if (existing.Status != GenerationStatus.Completed) return StageOutcome.Continue;

        _logger.LogInformation(
            "[Pipeline] Reusing existing run {RunId} for fingerprint {Fingerprint}. Status={Status}",
            existing.Id, context.Fingerprint, existing.Status);

        context.ShortCircuitOrchestrator = existing;
        return StageOutcome.ShortCircuitSuccess;
    }
}
