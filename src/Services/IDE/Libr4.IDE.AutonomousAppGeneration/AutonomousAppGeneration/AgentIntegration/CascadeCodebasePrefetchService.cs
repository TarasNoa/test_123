using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// Cascade orchestrator prefetch via <see cref="IFastContextPrefetcher"/> on a shallow-cloned upstream repo.
/// </summary>
public sealed class CascadeCodebasePrefetchService : ICascadeCodebasePrefetchService
{
    private readonly IFastContextPrefetcher _prefetcher;
    private readonly IUpstreamCloneProvider _cloneProvider;
    private readonly ILogger<CascadeCodebasePrefetchService> _logger;

    public CascadeCodebasePrefetchService(
        IFastContextPrefetcher prefetcher,
        IUpstreamCloneProvider cloneProvider,
        ILogger<CascadeCodebasePrefetchService> logger)
    {
        _prefetcher = prefetcher;
        _cloneProvider = cloneProvider;
        _logger = logger;
    }

    public async Task<string?> BuildPrefetchContextAsync(
        string userRequest,
        int maxChars,
        CancellationToken ct = default)
    {
        var cloneUrls = UpstreamCloneUrlResolver.ExtractCloneUrls(userRequest, max: 1);
        if (cloneUrls.Count == 0)
        {
            _logger.LogDebug("Cascade codebase-prefetch: no clone URLs in user request.");
            return null;
        }

        using var clone = await _cloneProvider.TryShallowCloneAsync(cloneUrls[0], ct).ConfigureAwait(false);
        if (clone is null)
            return null;

        var prefetch = await _prefetcher.PrefetchForRepairAsync(
            new FastContextPrefetchRequest(
                WorkspaceRoot: clone.WorkspaceRoot,
                BuildLog: null,
                Errors: Array.Empty<Libr4.IDE.Domain.AutonomousAppGeneration.ErrorReport>(),
                UserRequest: userRequest),
            ct).ConfigureAwait(false);

        if (prefetch.Hits.Count == 0)
            return null;

        var summary = JsonSerializer.Serialize(new
        {
            provider = "fast_context",
            tool = "search_codebase",
            mode = "cascade_prefetch",
            clone_url = clone.CloneUrl,
            queries = prefetch.Queries,
            confidence = prefetch.Confidence,
            hits = prefetch.Hits.Take(6).Select(h => new
            {
                path = h.Path,
                startLine = h.StartLine,
                endLine = h.EndLine,
                score = h.Score,
                matchKind = h.MatchKind,
                snippet = Truncate(h.Snippet, 240)
            }).ToArray()
        });

        return Truncate(summary, Math.Clamp(maxChars, 120, 4000));
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";
}
