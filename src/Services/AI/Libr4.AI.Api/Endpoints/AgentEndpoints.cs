using Libr4.AI.Application.Agents.Commands;
using Libr4.AI.Application.Agents.Queries;
using Libr4.AI.Domain.Agents;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai/agents")
            .WithTags("AI Agents")
            .WithOpenApi();

        // Get all agents (public)
        group.MapGet("/", async (
            [FromQuery] AgentType? type,
            [FromQuery] bool activeOnly = true,
            ISender sender = null!,
            CancellationToken ct = default) =>
        {
            var result = await sender.Send(new GetAgentsQuery(type, activeOnly), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        // Create agent (admin only)
        group.MapPost("/create", async (
            [FromBody] CreateAgentRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateAgentCommand(
                request.Name,
                request.Description,
                request.Type,
                request.Model,
                request.SystemPrompt), ct);

            return result.IsSuccess
                ? Results.Created($"/api/v1/ai/agents/{result.Value}", result.Value)
                : Results.BadRequest(result.Error);
        }).RequireAuthorization("Admin");

        return app;
    }
}

public record CreateAgentRequest(
    string Name,
    string Description,
    AgentType Type,
    string Model,
    string SystemPrompt);
