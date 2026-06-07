using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class DelegationReadTool : IAgentTool
{
    private readonly IDelegationManager _delegation;
    private readonly AgentRuntimeOptions _options;

    public DelegationReadTool(IDelegationManager delegation, IOptions<AgentRuntimeOptions> options)
    {
        _delegation = delegation;
        _options = options.Value;
    }

    public string Name => "delegation_read";
    public string Description => "Read delegation output. Input: { \"id\": \"brisk-blue-fox\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var runId = context.Session.RunId;
        if (runId is null)
            return Fail("run id unavailable");

        var id = input.TryGetProperty("id", out var i) && i.ValueKind == JsonValueKind.String ? i.GetString() : null;
        if (string.IsNullOrWhiteSpace(id))
            return Fail("id is required");

        var record = await _delegation.GetAsync(runId.Value, id!, ct).ConfigureAwait(false);
        if (record is null)
            return Fail($"delegation not found: {id}");

        var mdPath = Path.Combine(_options.RunsRoot, runId.Value.ToString("D"), "delegations", $"{id}.md");
        if (File.Exists(mdPath))
        {
            var content = await File.ReadAllTextAsync(mdPath, ct).ConfigureAwait(false);
            return new ToolExecutionResult(Name, true, content, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
        }

        return new ToolExecutionResult(
            Name,
            true,
            record.OutputPreview ?? record.Error ?? record.Status,
            Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("delegation_read", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
