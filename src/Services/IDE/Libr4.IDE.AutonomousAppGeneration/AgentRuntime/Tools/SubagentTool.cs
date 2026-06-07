using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Legacy alias for <see cref="TaskTool"/> (name: agent).</summary>
public sealed class SubagentTool : IAgentTool
{
    private readonly SubagentExecutionService _executor;
    private readonly AgentRuntimeOptions _options;

    public SubagentTool(SubagentExecutionService executor, IOptions<AgentRuntimeOptions> options)
    {
        _executor = executor;
        _options = options.Value;
    }

    public string Name => "agent";
    public string Description => "Delegate to subagent (legacy alias of task). Input: { \"name\": \"verify\", \"task\": \"...\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var task = input.TryGetProperty("task", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
        if (string.IsNullOrWhiteSpace(task))
            return Task.FromResult(Fail("task is required"));

        var name = input.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;
        var role = input.TryGetProperty("role", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString() : null;
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(role))
            return Task.FromResult(Fail("name or role required"));

        return _executor.ExecuteAsync(Name, name, role, task!, context, _options, ct);
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("agent", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
