using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.AutonomousAppGeneration.Host.Endpoints;

public sealed record McpInvokeHttpRequest(
    Guid? RunId,
    string ToolName,
    Dictionary<string, JsonElement>? Arguments);

public sealed record McpToolDescriptorDto(
    string ToolName,
    string ServerProfileKey,
    string Description,
    string Risk,
    string Lane,
    IReadOnlyList<string> Scopes);

/// <summary>
/// HTTP surface for MCP tool discovery and invocation (control plane).
/// </summary>
public static class McpIntegrationEndpoints
{
    public static void MapMcpIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/app-generation/mcp")
            .WithTags("MCP (Agent Bridge)")
            .WithOpenApi();

        group.MapGet("/tools", (IMcpToolRegistry registry) =>
        {
            var items = registry.ListTools()
                .Select(t => new McpToolDescriptorDto(
                    t.ToolName,
                    t.ServerProfileKey,
                    t.Description,
                    t.Risk.ToString(),
                    t.Lane.ToString(),
                    t.Scopes.ToList()))
                .ToList();
            return Results.Ok(items);
        })
        .WithName("ListMcpTools")
        .WithSummary("Registered MCP tools, risk tier, and server profile key.");

        group.MapPost("/invoke", async (
            [FromBody] McpInvokeHttpRequest body,
            IMcpToolInvocationService mcp,
            IAppGenerationRepository repository,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.ToolName))
                return Results.BadRequest(new { error = "toolName is required" });

            var args = McpArgumentMapper.ToObjectDictionary(body.Arguments);
            if (body.RunId is Guid runId)
            {
                var orchestrator = await repository.GetAsync(runId, ct).ConfigureAwait(false);
                if (orchestrator is null)
                    return Results.NotFound(new { error = "run_not_found", runId });

                var outcome = await mcp.InvokeAsync(orchestrator, body.ToolName.Trim(), args, ct)
                    .ConfigureAwait(false);
                await repository.SaveAsync(orchestrator, ct).ConfigureAwait(false);
                return Results.Ok(outcome);
            }

            var standalone = await mcp.InvokeStandaloneAsync(
                    userRequestContext: null,
                    body.ToolName.Trim(),
                    args,
                    ct)
                .ConfigureAwait(false);
            return Results.Ok(standalone);
        })
        .WithName("InvokeMcpTool")
        .WithSummary(
            "Call an MCP tool by name. When RunId is set, MCP audit rows are stored on that orchestrator run.");

        group.MapGet("/host/catalog", (IMcpHostCatalog catalog) => Results.Ok(new
        {
            tools = catalog.ListTools(),
            resources = catalog.ListResources(),
            prompts = catalog.ListPrompts(),
        }))
        .WithName("McpHostCatalog")
        .WithSummary("Unified MCP tool/resource/prompt catalog.");

        group.MapGet("/host/discovery", (IMcpRunHostManager host) =>
            Results.Ok(host.DiscoverServers()))
        .WithName("McpHostDiscovery")
        .WithSummary("External MCP server discovery and preflight status.");

        group.MapGet("/host/sessions", (IMcpRunHostManager host) =>
            Results.Ok(host.ListActiveSessions()))
        .WithName("McpHostSessions")
        .WithSummary("Active per-run MCP host sessions.");
    }
}

internal static class McpArgumentMapper
{
    public static IReadOnlyDictionary<string, object?> ToObjectDictionary(Dictionary<string, JsonElement>? raw)
    {
        if (raw is null || raw.Count == 0)
            return new Dictionary<string, object?>(StringComparer.Ordinal);

        return raw.ToDictionary(
            static kv => kv.Key,
            static kv => ToObject(kv.Value),
            StringComparer.Ordinal);
    }

    private static object? ToObject(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Undefined => null,
        JsonValueKind.Array => el,
        JsonValueKind.Object => el,
        _ => el.GetRawText(),
    };
}
