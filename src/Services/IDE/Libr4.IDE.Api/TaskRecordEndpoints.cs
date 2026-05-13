namespace Libr4.IDE.Api;

public static class TaskRecordEndpoints
{
    public static void MapTaskRecordEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/taskrecord")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "TaskRecord" }));
    }
}
