namespace Libr4.IDE.Api;

public static class SecurityTestingEndpoints
{
    public static void MapSecurityTestingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/securitytesting")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "SecurityTesting" }));
    }
}
