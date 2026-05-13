using Libr4.AI.Application.Agents;
using Libr4.AI.Domain.Agents;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.AI.Api.Endpoints;

public static class SubagentEndpoints
{
    public static void MapSubagentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subagents")
            .WithTags("Subagents");

        group.MapGet("/definitions", ([FromServices] SubagentService service) =>
        {
            return Results.Ok(service.GetAllSubagents());
        });

        group.MapGet("/definitions/{id}", (string id, [FromServices] SubagentService service) =>
        {
            var subagent = service.GetSubagent(id);
            return subagent is not null ? Results.Ok(subagent) : Results.NotFound();
        });

        group.MapGet("/definitions/category/{category}", (SubagentCategory category, [FromServices] SubagentService service) =>
        {
            return Results.Ok(service.GetSubagentsByCategory(category));
        });

        group.MapPost("/instances", (
            [FromBody] CreateInstanceRequest request,
            [FromServices] SubagentService service) =>
        {
            var instance = service.CreateSubagentInstance(request.SubagentId, request.ParentAgentId);
            return Results.Ok(instance);
        });

        group.MapGet("/instances/{instanceId}", (Guid instanceId, [FromServices] SubagentService service) =>
        {
            var instance = service.GetSubagentInstance(instanceId);
            return instance is not null ? Results.Ok(instance) : Results.NotFound();
        });

        group.MapGet("/instances/parent/{parentAgentId}", (Guid parentAgentId, [FromServices] SubagentService service) =>
        {
            return Results.Ok(service.GetInstancesByParentAgent(parentAgentId));
        });

        group.MapPost("/instances/{instanceId}/usage", (Guid instanceId, [FromServices] SubagentService service) =>
        {
            service.UpdateInstanceUsage(instanceId);
            return Results.Ok();
        });

        group.MapPost("/route", (
            [FromBody] RouteRequest request,
            [FromServices] SubagentService service) =>
        {
            var subagent = service.FindBestSubagentForTask(request.Task, request.Context);
            var modelTier = service.DetermineModelTier(request.Task, request.Context);

            return Results.Ok(new
            {
                subagent,
                modelTier,
                recommended = subagent?.Name ?? "No suitable subagent found"
            });
        });
    }

    public record CreateInstanceRequest(string SubagentId, Guid ParentAgentId);
    public record RouteRequest(string Task, string Context);
}
