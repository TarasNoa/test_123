namespace Libr4.IDE.Api;

public static class OrchestrationRunEndpoints
{
    public static void MapOrchestrationRunEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/orchestrationrun")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "OrchestrationRun" }));
    }
}
