/*
using Libr4.IDE.Application.Translation;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Translation service
/// Supports batch translation of text content
/// </summary>
public static class TranslationEndpoints
{
    public static void MapTranslationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ai/translate")
            .WithTags("Translation")
            .RequireAuthorization()
            .WithOpenApi();

        // Batch translate text
        group.MapPost("/batch", async (
            [FromBody] TranslateBatchRequest request,
            ITranslationService translationService,
            CancellationToken ct) =>
        {
            var translations = await translationService.TranslateBatchAsync(
                request.Items,
                request.TargetLanguage,
                ct);

            var response = new TranslateBatchResponse
            {
                Items = translations,
                TargetLanguage = request.TargetLanguage,
                SourceLanguage = request.SourceLanguage,
            };

            return Results.Ok(response);
        })
        .WithName("TranslateBatch")
        .WithSummary("Batch translate text")
        .WithDescription("Translates multiple text items to the target language")
        .WithOpenApi();
    }
}

// Request/Response DTOs
public record TranslateBatchRequest
{
    public string[] Items { get; init; } = Array.Empty<string>();
    public string TargetLanguage { get; init; } = "en";
    public string? SourceLanguage { get; init; }
    public string? Model { get; init; }
}

public record TranslateBatchResponse
{
    public string[] Items { get; init; } = Array.Empty<string>();
    public string TargetLanguage { get; init; } = string.Empty;
    public string? SourceLanguage { get; init; }
}
*/
