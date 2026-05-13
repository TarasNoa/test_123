namespace Libr4.IDE.Api;

public static class AutonomousRuntimePolicyEndpoints
{
    public static void MapAutonomousRuntimePolicyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/autonomousruntimepolicy")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "AutonomousRuntimePolicy" }));
    }
}
