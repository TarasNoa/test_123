using Libr4.Chat.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Libr4.Chat.Api.Endpoints;

public static class CodeShareEndpoints
{
    public static void MapCodeShareEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat/code")
            .WithTags("Code Sharing")
            .RequireAuthorization();

        group.MapPost("/snippets", async (
            [FromBody] CreateCodeSnippetRequest request,
            HttpContext context,
            ICodeSnippetService service) =>
        {
            var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException());

            try
            {
                var snippet = await service.CreateSnippetAsync(request, userId);
                return Results.Created($"/api/chat/code/snippets/{snippet.Id}", new { snippet });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to create snippet: {ex.Message}",
                    statusCode: 500,
                    title: "Code Snippet Error");
            }
        })
        .WithName("CreateCodeSnippet")
        .WithSummary("Create and share a code snippet");

        group.MapGet("/snippets/{snippetId}", async (
            Guid snippetId,
            ICodeSnippetService service) =>
        {
            try
            {
                var snippet = await service.GetSnippetAsync(snippetId);
                return Results.Ok(new { snippet });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to get snippet: {ex.Message}",
                    statusCode: 500,
                    title: "Code Snippet Error");
            }
        })
        .WithName("GetCodeSnippet")
        .WithSummary("Get a code snippet by ID");

        group.MapGet("/templates", async (
            ICodeSnippetService service) =>
        {
            var templates = await service.GetTemplatesAsync();
            return Results.Ok(new { templates });
        })
        .WithName("GetCodeTemplates")
        .WithSummary("Get code templates for quick sharing");
    }
}