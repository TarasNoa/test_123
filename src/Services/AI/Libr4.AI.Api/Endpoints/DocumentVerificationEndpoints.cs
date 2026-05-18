using Libr4.AI.Application.DocumentVerification;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class DocumentVerificationEndpoints
{
    public static IEndpointRouteBuilder MapDocumentVerificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/document-verification")
            .WithTags("Document Verification")
            .WithOpenApi()
            .RequireAuthorization();

        // Verify identity documents
        group.MapPost("/identity", async (
            [FromBody] IdentityVerificationRequest request,
            IDocumentVerificationService service,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.VerifyIdentityDocumentsAsync(request, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Identity verification failed: {ex.Message}",
                    statusCode: 500,
                    title: "Verification Error");
            }
        })
        .WithName("VerifyIdentity")
        .WithSummary("Verify passport and selfie using AI analysis");

        // Verify CV
        group.MapPost("/cv", async (
            [FromBody] CVVerificationRequest request,
            IDocumentVerificationService service,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.VerifyCVAsync(request, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"CV verification failed: {ex.Message}",
                    statusCode: 500,
                    title: "CV Verification Error");
            }
        })
        .WithName("VerifyCV")
        .WithSummary("Verify CV and LinkedIn profile using AI analysis");

        return app;
    }
}
