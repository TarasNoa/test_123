namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public interface IVerifyPassCheckpointService
{
    Task RecordVerifyPassAsync(Guid runId, Guid? shadowWorkspaceId, CancellationToken ct = default);

    Task<IReadOnlyList<RunDiffCheckpointSummary>> ListCheckpointsAsync(Guid runId, CancellationToken ct = default);

    Task<RunDiffCheckpointSnapshot?> LoadSnapshotAsync(Guid runId, string tag, CancellationToken ct = default);
}
