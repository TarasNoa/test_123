/*
using Libr4.IDE.Domain.MultiAgentOrchestration;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Agent Instance and Specialization
/// Enhanced with concepts from OpenAnalyst, AI-IDE-Agent, Roo-Code
/// </summary>
public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/agents")
            .WithTags("Agents")
            .RequireAuthorization();

        // Agent Instance operations
        group.MapPost("/instance/create", (
            [FromBody] AgentInstance instance,
            CancellationToken ct) =>
        {
            return Results.Ok(new { instanceId = instance.Id, type = instance.AgentType });
        })
        .WithName("CreateAgentInstance")
        .WithSummary("Create agent instance")
        .WithDescription("Create new AI agent instance with multi-mode operation support");

        group.MapPost("/instance/{instanceId}/mode", (
            Guid instanceId,
            AgentOperationMode mode,
            CancellationToken ct) =>
        {
            return Results.Ok(new { instanceId, mode });
        })
        .WithName("SwitchAgentMode")
        .WithSummary("Switch agent operation mode")
        .WithDescription("Switch agent to DataAnalyst, Code, Ask, Debug, or Custom mode");

        group.MapPost("/instance/{instanceId}/alert", (
            Guid instanceId,
            [FromBody] SmartAlert alert,
            CancellationToken ct) =>
        {
            return Results.Ok(new { instanceId, alertId = alert.Id });
        })
        .WithName("AddAgentAlert")
        .WithSummary("Add smart alert")
        .WithDescription("Add smart alert for task progress tracking");

        group.MapGet("/instance/{instanceId}/alerts", (
            Guid instanceId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { instanceId });
        })
        .WithName("GetAgentAlerts")
        .WithSummary("Get agent alerts")
        .WithDescription("Get all alerts for agent instance");

        group.MapPost("/instance/{instanceId}/checkpoint", (
            Guid instanceId,
            string name,
            string description,
            CancellationToken ct) =>
        {
            return Results.Ok(new { instanceId, name });
        })
        .WithName("CreateCheckpoint")
        .WithSummary("Create checkpoint")
        .WithDescription("Create state checkpoint for navigation (from Roo-Code)");

        group.MapPost("/instance/{instanceId}/checkpoint/{checkpointId}/restore", (
            Guid instanceId,
            Guid checkpointId,
            CancellationToken ct) =>
        {
            return Results.Ok(new { instanceId, checkpointId });
        })
        .WithName("RestoreCheckpoint")
        .WithSummary("Restore checkpoint")
        .WithDescription("Restore agent state from checkpoint");

        // Agent Specialization operations
        group.MapPost("/specialization/create", (
            [FromBody] AgentSpecialization specialization,
            CancellationToken ct) =>
        {
            return Results.Ok(new { specializationId = specialization.Id, name = specialization.Name });
        })
        .WithName("CreateAgentSpecialization")
        .WithSummary("Create agent specialization")
        .WithDescription("Create domain-specific agent specialization (from AI-IDE-Agent)");

        group.MapPost("/specialization/{specializationId}/expertise", (
            Guid specializationId,
            string expertise,
            CancellationToken ct) =>
        {
            return Results.Ok(new { specializationId, expertise });
        })
        .WithName("AddExpertise")
        .WithSummary("Add expertise")
        .WithDescription("Add domain expertise to specialization");

        group.MapGet("/specialization/category/{category}", (
            AgentDomainCategory category,
            CancellationToken ct) =>
        {
            return Results.Ok(new { category });
        })
        .WithName("GetSpecializationsByCategory")
        .WithSummary("Get specializations by category")
        .WithDescription("Get all specializations for specific domain category");

        group.MapGet("/specialization/most-used", (
            int topN = 5,
            CancellationToken ct) =>
        {
            return Results.Ok(new { topN });
        })
        .WithName("GetMostUsedSpecializations")
        .WithSummary("Get most used specializations")
        .WithDescription("Get most frequently used specializations");
    }
}
*/
