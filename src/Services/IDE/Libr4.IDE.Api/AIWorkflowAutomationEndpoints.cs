namespace Libr4.IDE.Api;

public static class AIWorkflowAutomationEndpoints
{
    public static void MapAIWorkflowAutomationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/aiworkflowautomation")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "AIWorkflowAutomation" }));
    }
}
