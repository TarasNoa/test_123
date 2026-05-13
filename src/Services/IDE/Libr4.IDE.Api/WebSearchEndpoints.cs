namespace Libr4.IDE.Api;

public static class WebSearchEndpoints
{
    public static void MapWebSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/websearch")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "WebSearch" }));
    }
}
