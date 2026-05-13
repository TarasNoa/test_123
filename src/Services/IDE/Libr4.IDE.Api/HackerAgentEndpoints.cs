namespace Libr4.IDE.Api;

public static class HackerAgentEndpoints
{
    public static void MapHackerAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/hackeragent")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "HackerAgent" }));
    }
}
