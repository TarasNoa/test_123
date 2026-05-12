using System.Text.Json;
using Libr4.AI.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class TranslationEndpoints
{
    public static IEndpointRouteBuilder MapTranslationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/translate")
            .WithTags("AI Translation")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/", async (
            [FromBody] TranslateTextRequest request,
            IAIService aiService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Results.Ok(new TranslateTextResponse(request.Text ?? string.Empty, request.TargetLanguage, request.SourceLanguage));
            }

            var prompt = BuildSingleTranslationPrompt(request.Text, request.TargetLanguage, request.SourceLanguage);
            var translated = await aiService.ChatAsync(prompt, BuildSystemPrompt(), request.Model);

            return Results.Ok(new TranslateTextResponse(
                Content: CleanTranslationResult(translated, request.Text),
                TargetLanguage: request.TargetLanguage,
                SourceLanguage: request.SourceLanguage));
        });

        group.MapPost("/batch", async (
            [FromBody] BatchTranslateTextRequest request,
            IAIService aiService,
            CancellationToken ct) =>
        {
            if (request.Items is null || request.Items.Count == 0)
            {
                return Results.Ok(new BatchTranslateTextResponse(Array.Empty<string>(), request.TargetLanguage, request.SourceLanguage));
            }

            var prompt = BuildBatchTranslationPrompt(request.Items, request.TargetLanguage, request.SourceLanguage);
            var translated = await aiService.ChatAsync(prompt, BuildSystemPrompt(), request.Model);
            var items = TryParseBatchTranslations(translated, request.Items);

            return Results.Ok(new BatchTranslateTextResponse(items, request.TargetLanguage, request.SourceLanguage));
        });

        return app;
    }

    private static string BuildSystemPrompt()
    {
        return """
            You are a precise translation engine for a freelance marketplace work chat.
            Translate naturally and accurately.
            Preserve code snippets, URLs, filenames, variable names, and markdown formatting.
            Do not explain anything.
            If text is already in the requested language, return it unchanged.
            """;
    }

    private static string BuildSingleTranslationPrompt(string text, string targetLanguage, string? sourceLanguage)
    {
        return $"""
            Translate the following chat message to {targetLanguage}.
            Source language: {(string.IsNullOrWhiteSpace(sourceLanguage) ? "auto-detect" : sourceLanguage)}.

            Return only the translated text, without quotes or comments.

            MESSAGE:
            {text}
            """;
    }

    private static string BuildBatchTranslationPrompt(IReadOnlyList<string> items, string targetLanguage, string? sourceLanguage)
    {
        var payload = JsonSerializer.Serialize(new
        {
            targetLanguage,
            sourceLanguage = string.IsNullOrWhiteSpace(sourceLanguage) ? "auto-detect" : sourceLanguage,
            items
        });

        return $$"""
            Translate every item in the JSON payload to {{targetLanguage}}.
            Keep the same order.
            Return strict JSON only in the form:
            {"translations":["...", "..."]}

            JSON PAYLOAD:
            {{payload}}
            """;
    }

    private static string CleanTranslationResult(string raw, string fallback)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed.Trim('`').Trim();
        }

        return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
    }

    private static IReadOnlyList<string> TryParseBatchTranslations(string raw, IReadOnlyList<string> fallback)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstBrace = trimmed.IndexOf('{');
            var lastBrace = trimmed.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                trimmed = trimmed[firstBrace..(lastBrace + 1)];
            }
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<BatchTranslationModel>(trimmed);
            if (parsed?.Translations is { Count: > 0 } translations)
            {
                return translations.Count == fallback.Count
                    ? translations
                    : AlignTranslations(translations, fallback);
            }
        }
        catch (Exception ex)
        {
            // Log parsing error but fall back to original texts
            Serilog.Log.Debug(ex, "Failed to parse batch translation JSON, using fallback");
        }

        return fallback;
    }

    private static IReadOnlyList<string> AlignTranslations(IReadOnlyList<string> translations, IReadOnlyList<string> fallback)
    {
        var result = new string[fallback.Count];
        for (var i = 0; i < fallback.Count; i++)
        {
            result[i] = i < translations.Count && !string.IsNullOrWhiteSpace(translations[i])
                ? translations[i]
                : fallback[i];
        }

        return result;
    }

    private sealed record BatchTranslationModel(IReadOnlyList<string> Translations);
}

public sealed record TranslateTextRequest(
    string Text,
    string TargetLanguage,
    string? SourceLanguage = null,
    string? Model = null);

public sealed record TranslateTextResponse(
    string Content,
    string TargetLanguage,
    string? SourceLanguage);

public sealed record BatchTranslateTextRequest(
    IReadOnlyList<string> Items,
    string TargetLanguage,
    string? SourceLanguage = null,
    string? Model = null);

public sealed record BatchTranslateTextResponse(
    IReadOnlyList<string> Items,
    string TargetLanguage,
    string? SourceLanguage);
