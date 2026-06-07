namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public interface IEvidenceDiffCorrelator
{
    Task<FileDiffEvidenceResponse?> GetForPathAsync(Guid runId, string path, CancellationToken ct = default);

    Task<DiffEvidenceOverlayIndex> GetOverlaysAsync(Guid runId, CancellationToken ct = default);
}
