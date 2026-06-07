namespace Libr4.IDE.Application.AutonomousAppGeneration.FastContext;

public sealed record FastContextPrefetchRequest(
    string? WorkspaceRoot,
    string? BuildLog,
    IReadOnlyList<Libr4.IDE.Domain.AutonomousAppGeneration.ErrorReport> Errors,
    IReadOnlyList<Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile>? MemoryFiles = null,
    string? UserRequest = null,
    Guid? RunId = null);

public sealed record FastContextPrefetchResult(
    IReadOnlyList<CodebaseSearchHit> Hits,
    double Confidence,
    IReadOnlyList<string> Queries,
    string FormattedText)
{
    public bool MeetsContextPackThreshold(double threshold) => Confidence >= threshold && Hits.Count > 0;
}

public interface IFastContextPrefetcher
{
    Task<FastContextPrefetchResult> PrefetchForRepairAsync(
        FastContextPrefetchRequest request,
        CancellationToken ct = default);

    Task WarmIndexAsync(string workspaceRoot, Guid? runId = null, CancellationToken ct = default);
}
