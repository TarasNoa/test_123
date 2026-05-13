namespace Libr4.IDE.Api;

public static class LLMRouterEndpoints
{
    public static void MapLLMRouterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/llmrouter")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "LLMRouter" }));
    }
}
