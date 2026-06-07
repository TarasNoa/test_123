using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Backward-compatible alias for activate_skill.</summary>
public sealed class SkillAgentTool : IAgentTool
{
    private readonly ActivateSkillTool _activate;

    public SkillAgentTool(ActivateSkillTool activate) => _activate = activate;

    public string Name => "skill";
    public string Description => "Alias for activate_skill. Input: { \"name\": \"python-django\" }";
    public bool IsReadOnly => _activate.IsReadOnly;
    public bool IsConcurrencySafe(JsonElement input) => _activate.IsConcurrencySafe(input);

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct) =>
        _activate.ExecuteAsync(input, context, ct);
}
