using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

/// <summary>
/// Native cascade web-prefetch via <see cref="IAgentObscuraTool.ResearchAsync"/> (browser_research).
/// </summary>
public sealed class CascadeWebPrefetchService : ICascadeWebPrefetchService
{
    private readonly IAgentObscuraTool _obscura;
    private readonly ILogger<CascadeWebPrefetchService> _logger;

    public CascadeWebPrefetchService(
        IAgentObscuraTool obscura,
        ILogger<CascadeWebPrefetchService> logger)
    {
        _obscura = obscura;
        _logger = logger;
    }

    public async Task<string?> BuildPrefetchContextAsync(
        string userRequest,
        int maxChars,
        CancellationToken ct = default)
    {
        var sources = BrowserUrlClassifier.ExtractHttpUrls(userRequest, max: 3);
        if (sources.Count == 0)
        {
            _logger.LogDebug("Cascade web-prefetch: no URLs discovered in user request.");
            return null;
        }

        var query = Truncate(userRequest, 180);
        var stealth = BrowserUrlClassifier.RequiresStealthMode(sources);

        try
        {
            var research = await _obscura.ResearchAsync(
                query,
                sources.ToArray(),
                new WebResearchOptions
                {
                    StealthMode = stealth,
                    MaxSources = sources.Count,
                    WaitAfterLoadMs = 1_500
                },
                ct).ConfigureAwait(false);

            if (research.SuccessfulSources == 0)
                return null;

            var summary = JsonSerializer.Serialize(new
            {
                provider = "obscura",
                tool = BrowserToolNames.Research,
                mode = "cascade_prefetch",
                query,
                stealth_mode = stealth,
                sources = research.Sources.Select(s => new
                {
                    url = s.Url,
                    title = s.Title,
                    relevance = s.RelevanceScore,
                    text = Truncate(s.Content, 400)
                }).ToArray()
            });

            return Truncate(summary, Math.Clamp(maxChars, 120, 4000));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Cascade native web-prefetch failed.");
            return null;
        }
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "…";
}
