using Libr4.AI.Infrastructure.SandboxExecutor;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class ExecutorEndpoints
{
    public static void MapExecutorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/executor")
            .WithTags("Sandbox Executor");

        group.MapPost("/execute", async (
            [FromBody] ExecuteRequest request,
            SandboxExecutorService service) =>
        {
            try
            {
                var result = service.Execute(request.Language, request.Code);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });
    }

    public record ExecuteRequest(string Language, string Code);
}
