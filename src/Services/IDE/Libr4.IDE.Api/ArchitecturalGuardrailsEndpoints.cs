namespace Libr4.IDE.Api;

public static class ArchitecturalGuardrailsEndpoints
{
    public static void MapArchitecturalGuardrailsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/architecturalguardrails")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "ArchitecturalGuardrails" }));
    }
}
