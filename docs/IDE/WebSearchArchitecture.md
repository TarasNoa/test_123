# IDE Agent Web Search Service - Architecture Documentation

## Overview

The Web Search Service provides agents with web search capabilities using multiple providers (Tavily, Brave, SerpAPI, DuckDuckGo). It aggregates results, filters for relevance, and returns structured search data. Uses F# for result aggregation and relevance scoring algorithms.

## Architecture

### Technology Stack

- **C#** - Domain models, API, validation (Infrastructure)
- **F#** - Result aggregation, relevance scoring, provider routing (Algorithms)
- **MediatR** - CQRS pattern for Application layer
- **Minimal APIs** - REST endpoints

### Layer Structure

```
Libr4.IDE.Domain/
  └── WebSearch/
      ├── SearchProvider.cs              # Enum (Tavily, Brave, SerpAPI, DuckDuckGo)
      ├── SearchResult.cs                # Entity representing a search result
      ├── WebSearch.cs                   # AggregateRoot for web search
      └── Events/
          ├── SearchStartedEvent.cs
          ├── ResultsReceivedEvent.cs
          └── SearchCompletedEvent.cs

Libr4.IDE.Domain.Algorithms/
  └── WebSearchAlgorithms.fs
      ├── ExecuteSearch                 # Execute search on provider
      ├── AggregateResults              # Aggregate results from multiple providers
      /// CalculateRelevanceScore        # Calculate relevance score for results
      /// FilterResults                  # Filter results by relevance
      └── RouteSearch                  # Route search to optimal provider

Libr4.IDE.Application/
  └── WebSearch/
      ├── Commands/
          ├── ExecuteSearchCommand.cs
      ├── Queries/
          ├── GetSearchResultsQuery.cs
      ├── DTOs/
          ├── SearchResultDto.cs
          ├── WebSearchDto.cs
      ├── Handlers/
          ├── ExecuteSearchCommandHandler.cs
          ├── GetSearchResultsQueryHandler.cs
      └── Validators/
          └── ExecuteSearchCommandValidator.cs

Libr4.IDE.Api/
  └── WebSearchEndpoints.cs              # Minimal API endpoints
```

## Domain Model

### SearchProvider Enum

```csharp
public enum SearchProvider
{
    Tavily,
    Brave,
    SerpAPI,
    DuckDuckGo
}
```

### SearchResult Entity

```csharp
public class SearchResult
{
    public Guid Id { get; }
    public string Title { get; }
    public string Url { get; }
    public string Snippet { get; }
    public double RelevanceScore { get; }
    public SearchProvider Provider { get; }
}
```

### WebSearch AggregateRoot

```csharp
public class WebSearch : AggregateRoot<Guid>
{
    public string SearchId { get; }
    public string Query { get; }
    public List<SearchResult> Results { get; }
    public string Status { get; }
    public DateTime CreatedAt { get; }
}
```

## F# Algorithms

### ExecuteSearch

Executes search on provider:

```fsharp
let executeSearch (provider: SearchProvider) (query: string) : SearchResult list =
    // Call provider API
    // Parse results
    // Return search results
```

### AggregateResults

Aggregates results from multiple providers:

```fsharp
let aggregateResults (results: (SearchProvider * SearchResult list) list) : SearchResult list =
    // Combine results from all providers
    // Remove duplicates
    // Sort by relevance
```

## Application Layer (C#)

### Command Handler

```csharp
public class ExecuteSearchCommandHandler : IRequestHandler<ExecuteSearchCommand, WebSearchDto>
{
    private readonly IWebSearchAlgorithms _algorithms;
    
    public async Task<WebSearchDto> Handle(ExecuteSearchCommand request, CancellationToken ct)
    {
        // Call F# algorithm
        var search = _algorithms.RouteSearch(
            request.Query,
            request.Providers
        );
        
        // Map to DTO
        return search.ToDto();
    }
}
```

## API Layer (C#)

### Minimal API Endpoints

```csharp
public static class WebSearchEndpoints
{
    public static void MapWebSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/web-search")
            .WithTags("Web Search")
            .RequireAuthorization();
        
        group.MapPost("/search", async (
            ExecuteSearchCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("ExecuteWebSearch")
        .WithOpenApi();
    }
}
```

## Search Providers

| Provider | API Type | Cost | Features |
|----------|----------|------|----------|
| Tavily | REST API | Paid | AI-powered search |
| Brave | REST API | Free/Paid | Privacy-focused |
| SerpAPI | REST API | Paid | Google results |
| DuckDuckGo | REST API | Free | Privacy-focused |

## Testing Strategy

1. **Unit Tests** - Test F# aggregation algorithms
2. **Integration Tests** - Test C# Application layer
3. **E2E Tests** - Test full search flow

## Performance Considerations

- Web search can be slow due to network latency
- Use caching for common queries
- Implement parallel searches across providers
- Rate limit per provider

## Security Considerations

- Validate all search queries
- Sanitize search results
- Rate limit per user
- Audit all search operations

## Next Steps

1. Implement Domain layer (C#)
2. Implement F# algorithms
3. Implement Application layer (C#)
4. Implement API layer (C#)
5. Add unit tests
6. Add integration tests
7. Add documentation
