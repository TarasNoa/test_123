namespace Libr4.IDE.Api;

public static class TaskDecompositionEndpoints
{
    public static void MapTaskDecompositionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/taskdecomposition")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "TaskDecomposition" }));
    }
}
