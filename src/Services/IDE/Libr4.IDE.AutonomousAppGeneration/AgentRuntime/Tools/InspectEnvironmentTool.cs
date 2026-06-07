using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class InspectEnvironmentTool : IAgentTool
{
    public string Name => "inspect_environment";
    public string Description => "Host/workspace toolchain snapshot (python, pip, node, docker). Input: {}";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var json = BuildEnvironmentInspector.Inspect(context.Workspace.HostPath);
        return Task.FromResult(new ToolExecutionResult(Name, true, json, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>()));
    }
}
