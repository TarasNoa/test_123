namespace Libr4.IDE.Api;

public static class SemanticCodeGraphEndpoints
{
    public static void MapSemanticCodeGraphEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/semanticcodegraph")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "SemanticCodeGraph" }));
    }
}
