using Libr4.IDE.Domain.MultiAgentOrchestration;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/agents")
            .RequireAuthorization();

        group.MapPost("/instance/create", (
            [FromBody] AgentInstance instance,
            CancellationToken ct) =>
        {
            return Results.Ok(new { instanceId = instance.Id, type = instance.AgentType });
        });

        group.MapPost("/instance/{instanceId}/mode", (
            Guid instanceId,
            AgentOperationMode mode,
            CancellationToken ct) =>
        {
            return Results.Ok(new { instanceId, mode });
        });

        group.MapPost("/instance/{instanceId}/alert", (
            Guid instanceId,
            [FromBody] SmartAlert alert,
            CancellationToken ct) =>
        {
            return Results.Ok(new { instanceId, alertId = alert.Id });
        });

        group.MapGet("/instance/{instanceId}/alerts", (
            Guid instanceId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { instanceId });
        });

        group.MapPost("/instance/{instanceId}/checkpoint", (
            Guid instanceId,
            string name,
            string description,
            CancellationToken ct) =>
        {
            return Results.Ok(new { instanceId, name });
        });

        group.MapPost("/instance/{instanceId}/checkpoint/{checkpointId}/restore", (
            Guid instanceId,
            Guid checkpointId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { instanceId, checkpointId });
        });

        group.MapPost("/specialization/create", (
            [FromBody] AgentSpecialization specialization,
            CancellationToken ct) =>
        {
            return Results.Ok(new { specializationId = specialization.Id, name = specialization.Name });
        });

        group.MapPost("/specialization/{specializationId}/expertise", (
            Guid specializationId,
            string expertise,
            CancellationToken ct) =>
        {
            return Results.Ok(new { specializationId, expertise });
        });

        group.MapGet("/specialization/category/{category}", (
            AgentDomainCategory category,
            CancellationToken ct) =>
        {
            return Results.Ok(new { category });
        });

        group.MapGet("/specialization/most-used", (
            CancellationToken ct,
            int topN = 5) =>
        {
            return Results.Ok(new { topN });
        });
    }
}
