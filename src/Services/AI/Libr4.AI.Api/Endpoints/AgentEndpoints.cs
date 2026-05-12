using Libr4.AI.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai/agents")
            .WithTags("AI Agents")
            .RequireAuthorization();

        group.MapGet("/", async (IAgentService agentService) =>
        {
            var agents = await agentService.GetAgentsAsync();
            return Results.Ok(new { agents });
        })
        .WithName("GetAgents")
        .WithSummary("Get all available AI agents");

        group.MapPost("/", async (
            [FromBody] CreateAgentRequest request,
            IAgentService agentService) =>
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Agent name is required" });
            }

            try
            {
                var agent = await agentService.CreateAgentAsync(request);
                return Results.Created($"/api/ai/agents/{agent.Id}", new { agent });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to create agent: {ex.Message}",
                    statusCode: 500,
                    title: "Agent Creation Error");
            }
        })
        .WithName("CreateAgent")
        .WithSummary("Create a new AI agent");

        group.MapGet("/{id}", async (
            Guid id,
            IAgentService agentService) =>
        {
            try
            {
                var agent = await agentService.GetAgentByIdAsync(id);
                if (agent == null)
                {
                    return Results.NotFound(new { error = "Agent not found" });
                }
                return Results.Ok(new { agent });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to get agent: {ex.Message}",
                    statusCode: 500,
                    title: "Agent Retrieval Error");
            }
        })
        .WithName("GetAgentById")
        .WithSummary("Get a specific AI agent by ID");

        group.MapPut("/{id}/activate", async (
            Guid id,
            IAgentService agentService) =>
        {
            try
            {
                await agentService.ActivateAgentAsync(id);
                return Results.Ok(new { message = "Agent activated" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to activate agent: {ex.Message}",
                    statusCode: 500,
                    title: "Agent Activation Error");
            }
        })
        .WithName("ActivateAgent")
        .WithSummary("Activate an AI agent");

        group.MapPut("/{id}/deactivate", async (
            Guid id,
            IAgentService agentService) =>
        {
            try
            {
                await agentService.DeactivateAgentAsync(id);
                return Results.Ok(new { message = "Agent deactivated" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to deactivate agent: {ex.Message}",
                    statusCode: 500,
                    title: "Agent Deactivation Error");
            }
        })
        .WithName("DeactivateAgent")
        .WithSummary("Deactivate an AI agent");

        group.MapGet("/health", () => Results.Ok(new { status = "AI Agents is healthy", timestamp = DateTimeOffset.UtcNow }))
            .WithName("AgentsHealth")
            .WithSummary("Health check for AI Agents service");
    }
}

