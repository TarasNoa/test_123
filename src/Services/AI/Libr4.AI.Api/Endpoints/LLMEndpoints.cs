using Libr4.AI.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class LLMEndpoints
{
    public static void MapLLMEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai/llm")
            .WithTags("LLM")
            .RequireAuthorization();

        group.MapPost("/generate-code", async (
            [FromBody] GenerateCodeRequest request,
            ILLMService llmService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return Results.BadRequest(new { error = "Prompt is required" });
            }

            try
            {
                var code = await llmService.GenerateCodeAsync(request.Prompt);
                return Results.Ok(new { generatedCode = code });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to generate code: {ex.Message}",
                    statusCode: 500,
                    title: "LLM Error");
            }
        })
        .WithName("GenerateCode")
        .WithSummary("Generate code using LLM");

        group.MapPost("/explain-code", async (
            [FromBody] ExplainCodeRequest request,
            ILLMService llmService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return Results.BadRequest(new { error = "Code is required" });
            }

            try
            {
                var explanation = await llmService.ExplainCodeAsync(request.Code);
                return Results.Ok(new { explanation });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to explain code: {ex.Message}",
                    statusCode: 500,
                    title: "LLM Error");
            }
        })
        .WithName("ExplainCode")
        .WithSummary("Explain code using LLM");

        group.MapPost("/embeddings", async (
            [FromBody] EmbeddingsRequest request,
            ILLMService llmService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Results.BadRequest(new { error = "Text is required" });
            }

            try
            {
                var embeddings = await llmService.GetEmbeddingsAsync(request.Text);
                return Results.Ok(new { embeddings });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to generate embeddings: {ex.Message}",
                    statusCode: 500,
                    title: "LLM Error");
            }
        })
        .WithName("GetEmbeddings")
        .WithSummary("Generate embeddings for text using LLM");
    }
}

// Request DTOs
public record GenerateCodeRequest(string Prompt);
public record ExplainCodeRequest(string Code);
public record EmbeddingsRequest(string Text);