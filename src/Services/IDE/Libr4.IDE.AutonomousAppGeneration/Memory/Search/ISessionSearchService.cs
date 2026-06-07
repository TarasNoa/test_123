namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Search;

public sealed record SessionSearchHit(
    string Source,
    Guid RunId,
    int? StepNumber,
    string? ToolName,
    string? MemoryKey,
    string? MemoryKind,
    string Snippet,
    double Score);

public interface ISessionSearchService
{
    Task<IReadOnlyList<SessionSearchHit>> SearchAsync(string query, int limit = 25, CancellationToken ct = default);
}
