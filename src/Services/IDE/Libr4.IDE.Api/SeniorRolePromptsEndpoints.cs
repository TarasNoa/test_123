namespace Libr4.IDE.Api;

public static class SeniorRolePromptsEndpoints
{
    public static void MapSeniorRolePromptsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/seniorroleprompts")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "SeniorRolePrompts" }));
    }
}
