using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Claude Code MCPTool — invoke registered MCP tool from agent loop.</summary>
public sealed class McpAgentTool : IAgentTool
{
    private readonly IServiceScopeFactory _scopes;

    public McpAgentTool(IServiceScopeFactory scopes) => _scopes = scopes;

    public string Name => "mcp";
    public string Description => "Invoke MCP tool. Input: { \"tool\": \"name\", \"arguments\": {} }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var toolName = input.TryGetProperty("tool", out var t) && t.ValueKind == JsonValueKind.String
            ? t.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(toolName))
            return Fail("tool name required");

        var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (input.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in argsEl.EnumerateObject())
                args[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.ToString();
        }

        using var scope = _scopes.CreateScope();
        var mcp = scope.ServiceProvider.GetService<IMcpToolInvocationService>();
        if (mcp is null)
            return Fail("MCP service not configured");

        var ctx = context.Plan?.ApplicationDescription ?? context.Plan?.ApplicationName ?? string.Empty;
        var outcome = await mcp.InvokeStandaloneAsync(ctx, toolName, args, ct).ConfigureAwait(false);
        var output = outcome.Succeeded
            ? outcome.ResultSummary ?? outcome.Detail ?? "(empty)"
            : $"MCP error: {outcome.OutcomeCode}: {outcome.Detail ?? outcome.ResultSummary}";
        return new ToolExecutionResult(Name, outcome.Succeeded, output, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("mcp", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
