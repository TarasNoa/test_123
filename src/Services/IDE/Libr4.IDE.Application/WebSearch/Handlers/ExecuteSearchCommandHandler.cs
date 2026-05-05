/*
using MediatR;
using Libr4.IDE.Application.WebSearch.Commands;
using Libr4.IDE.Application.WebSearch.DTOs;
using Libr4.AI.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;

namespace Libr4.IDE.Application.WebSearch.Handlers;

/// <summary>
/// Handler for ExecuteSearchCommand - Multi-provider web search
/// </summary>
public class ExecuteSearchCommandHandler : IRequestHandler<ExecuteSearchCommand, WebSearchDto>
{
    private readonly IAIService _aiService;
    private readonly ILogger<ExecuteSearchCommandHandler> _logger;
    private readonly HttpClient _httpClient;

    public ExecuteSearchCommandHandler(
        IAIService aiService,
        ILogger<ExecuteSearchCommandHandler> logger,
        HttpClient httpClient)
    {
        _aiService = aiService;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<WebSearchDto> Handle(ExecuteSearchCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Executing web search: {Query}", request.Query);

        var results = new List<SearchResultDto>();
        var searchId = Guid.NewGuid().ToString();

        // Use AI for search if no external providers configured
        if (request.Providers.Count == 0 || request.Providers.Contains(SearchProvider.AI))
        {
            var aiResults = await SearchWithAIAsync(request.Query, ct);
            results.AddRange(aiResults);
        }

        // Simulated external search (would integrate with Bing/Google APIs)
        if (request.Providers.Contains(SearchProvider.Bing))
        {
            var bingResults = await SearchBingAsync(request.Query, ct);
            results.AddRange(bingResults);
        }

        return new WebSearchDto
        {
            Id = Guid.NewGuid(),
            SearchId = searchId,
            Query = request.Query,
            Results = results.Take(10).ToList(),
            TotalCount = results.Count,
            SearchedAt = DateTime.UtcNow
        };
    }

    private async Task<List<SearchResultDto>> SearchWithAIAsync(string query, CancellationToken ct)
    {
        var results = new List<SearchResultDto>();

        try
        {
            var prompt = $@"Search for information about: {query}

Provide 5 relevant results with:
- Title
- URL (plausible)
- Snippet/summary
- Source type (documentation, article, forum, etc.)

Format: Title|URL|Snippet|SourceType";

            var response = await _aiService.GenerateCompletionAsync(prompt, cancellationToken: ct);

            foreach (var line in response.Split('\n').Where(l => l.Contains('|')))
            {
                var parts = line.Split('|');
                if (parts.Length >= 3)
                {
                    results.Add(new SearchResultDto
                    {
                        Id = Guid.NewGuid(),
                        Title = parts[0].Trim(),
                        Url = parts[1].Trim(),
                        Snippet = parts[2].Trim(),
                        Source = parts.Length > 3 ? parts[3].Trim() : "AI",
                        RelevanceScore = 0.9
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI search failed");
        }

        return results;
    }

    private async Task<List<SearchResultDto>> SearchBingAsync(string query, CancellationToken ct)
    {
        // Placeholder for Bing API integration
        // Would use: https://api.bing.microsoft.com/v7.0/search
        _logger.LogDebug("Bing search placeholder - would call Bing API");
        return new List<SearchResultDto>();
    }
}
*/
