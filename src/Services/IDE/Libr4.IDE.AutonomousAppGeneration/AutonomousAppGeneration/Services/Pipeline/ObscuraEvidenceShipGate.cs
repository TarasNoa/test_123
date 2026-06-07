using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

public sealed class ShipStageOptions
{
    public const string SectionName = "AutonomousAppGeneration:Ship";

    /// <summary>Hard-block ShipStage when verify_passed is not true in generation context.</summary>
    public bool RequireVerifyPass { get; set; } = true;

    /// <summary>Hard-block ShipStage when Obscura evidence manifest has no artifacts (production default).</summary>
    public bool RequireObscuraEvidenceManifest { get; set; } = true;
}

public sealed record ShipEvidenceGateResult(bool Allowed, string? BlockReason, int ObscuraArtifactCount = 0);

public interface IObscuraEvidenceShipGate
{
    Task<ShipEvidenceGateResult> EvaluateAsync(GenerationContext context, CancellationToken ct = default);
}

public sealed class ObscuraEvidenceShipGate : IObscuraEvidenceShipGate
{
    private readonly ShipStageOptions _shipOptions;
    private readonly AutonomousBenchmarkModeOptions _benchmarkOptions;
    private readonly AutonomousPlatformUtilizationOptions _platformOptions;
    private readonly IObscuraEvidenceStore? _obscuraEvidence;

    public ObscuraEvidenceShipGate(
        IOptions<ShipStageOptions> shipOptions,
        IOptions<AutonomousBenchmarkModeOptions> benchmarkOptions,
        IOptions<AutonomousPlatformUtilizationOptions> platformOptions,
        IObscuraEvidenceStore? obscuraEvidence = null)
    {
        _shipOptions = shipOptions.Value;
        _benchmarkOptions = benchmarkOptions.Value;
        _platformOptions = platformOptions.Value;
        _obscuraEvidence = obscuraEvidence;
    }

    public async Task<ShipEvidenceGateResult> EvaluateAsync(GenerationContext context, CancellationToken ct = default)
    {
        if (BenchmarkExecutionPathPolicy.ShouldDeferFailedGate(
                _benchmarkOptions,
                BenchmarkExecutionPathPolicy.Stages.Ship,
                _platformOptions))
        {
            return new ShipEvidenceGateResult(true, null);
        }

        var verifyPassed = context.Items.TryGetValue("verify_passed", out var passedObj) && passedObj is true;
        if (_shipOptions.RequireVerifyPass && !verifyPassed)
            return new ShipEvidenceGateResult(false, "verify_not_passed");

        if (!_shipOptions.RequireObscuraEvidenceManifest)
            return new ShipEvidenceGateResult(true, null);

        if (_obscuraEvidence is null)
            return new ShipEvidenceGateResult(false, "obscura_evidence_store_unavailable");

        var runId = context.Orchestrator.Id;
        var manifest = await _obscuraEvidence.GetManifestAsync(runId, ct).ConfigureAwait(false);
        var artifactCount = manifest.Artifacts.Count;
        if (artifactCount == 0)
            artifactCount = _obscuraEvidence.List(runId).Artifacts.Count;

        if (artifactCount == 0)
            return new ShipEvidenceGateResult(false, "obscura_evidence_missing");

        return new ShipEvidenceGateResult(true, null, artifactCount);
    }
}
