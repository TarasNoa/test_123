namespace Libr4.IDE.Api;

public static class CodeIntelligenceEndpoints
{
    public static void MapCodeIntelligenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/codeintelligence")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "CodeIntelligence" }));
    }
}
