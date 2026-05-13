namespace Libr4.IDE.Api;

public static class SemanticBlameEndpoints
{
    public static void MapSemanticBlameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/semanticblame")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "SemanticBlame" }));
    }
}
