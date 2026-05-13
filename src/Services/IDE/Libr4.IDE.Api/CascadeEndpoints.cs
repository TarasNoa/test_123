namespace Libr4.IDE.Api;

public static class CascadeEndpoints
{
    public static void MapCascadeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/cascade")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "Cascade" }));
    }
}
