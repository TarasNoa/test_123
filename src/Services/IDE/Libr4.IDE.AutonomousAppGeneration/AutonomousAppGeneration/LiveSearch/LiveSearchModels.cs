namespace Libr4.IDE.Application.AutonomousAppGeneration.LiveSearch;

public sealed record LiveSearchHit(
    string Title,
    string Url,
    string Snippet,
    string Provider);

public sealed record LiveSearchResponse(
    string Query,
    string Provider,
    bool FromCache,
    IReadOnlyList<LiveSearchHit> Hits,
    string? TruncationNotice = null);

public sealed record LiveSearchRequest(
    string Query,
    string? SessionKey = null,
    int? MaxResults = null);

public interface ILiveSearchService
{
    Task<LiveSearchResponse> SearchWebAsync(LiveSearchRequest request, CancellationToken ct = default);

    Task<LiveSearchResponse> SearchXAsync(LiveSearchRequest request, CancellationToken ct = default);
}

public interface ILiveWebSearchBackend
{
    string ProviderName { get; }

    Task<IReadOnlyList<LiveSearchHit>> SearchAsync(string query, int maxResults, CancellationToken ct);
}

public interface ILiveXSearchBackend
{
    Task<IReadOnlyList<LiveSearchHit>> SearchAsync(string query, int maxResults, CancellationToken ct);
}
