namespace Libr4.IDE.Api;

public static class IntelligenceRouterEndpoints
{
    public static void MapIntelligenceRouterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/intelligencerouter")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "IntelligenceRouter" }));
    }
}
