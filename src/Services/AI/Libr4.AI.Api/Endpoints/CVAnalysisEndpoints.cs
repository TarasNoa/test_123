using Libr4.AI.Application.CVAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class CVAnalysisEndpoints
{
    public static IEndpointRouteBuilder MapCVAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai")
            .WithTags("CV Analysis")
            .WithOpenApi()
            .AllowAnonymous();

        group.MapPost("/cv-analysis", async (
            [FromBody] CVAnalysisRequest request,
            ICVAnalysisService service,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.AnalyzeAsync(request, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"CV analysis failed: {ex.Message}",
                    statusCode: 500,
                    title: "CV Analysis Error");
            }
        })
        .WithName("AnalyzeCV")
        .WithSummary("Analyze CV + LinkedIn profile to extract skills and determine proficiency levels");

        return app;
    }
}
