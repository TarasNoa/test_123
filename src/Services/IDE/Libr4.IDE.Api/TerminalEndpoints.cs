namespace Libr4.IDE.Api;

public static class TerminalEndpoints
{
    public static void MapTerminalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/terminal")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "Terminal" }));
    }
}
