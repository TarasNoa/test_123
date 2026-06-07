namespace Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;

public interface IRunDiffAggregator
{
    Task<RunDiffListResponse> ListAsync(Guid runId, RunDiffQuery query, CancellationToken ct = default);

    Task<RunFileDiffDetail?> GetDetailAsync(
        Guid runId,
        string path,
        string? checkpointTag = null,
        CancellationToken ct = default);

    Task<RunDiffCheckpointListResponse> ListCheckpointsAsync(Guid runId, CancellationToken ct = default);
}
