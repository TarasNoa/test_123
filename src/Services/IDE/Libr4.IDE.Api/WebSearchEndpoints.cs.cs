/*
using Libr4.IDE.Application.WebSearch.Commands;
using Libr4.IDE.Application.WebSearch.Validators;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Web Search
/// </summary>
public static class WebSearchEndpoints
{
    public static void MapWebSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/web-search")
            .WithTags("Web Search")
            .RequireAuthorization()
            .WithOpenApi();
        
        group.MapPost("/search", async (
            [FromBody] ExecuteSearchCommand command,
            IMediator mediator,
            ExecuteSearchCommandValidator validator,
            CancellationToken ct) =>
        {
            // Validate command
            var validationResult = await validator.ValidateAsync(command, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }
            
            // Execute command
            var result = await mediator.Send(command, ct);
            
            return Results.Ok(result);
        })
        .WithName("ExecuteWebSearch")
        .WithSummary("Execute web search")
        .WithDescription("Executes web search using multiple providers (Tavily, Brave, SerpAPI, DuckDuckGo) with result aggregation")
        .WithOpenApi();
    }
}
*/
