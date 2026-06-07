using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class EnterPlanModeTool : IAgentTool
{
    public string Name => "enter_plan_mode";
    public string Description => "Switch to read-only plan mode before making edits. Input: {}";
    public bool IsReadOnly => false;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        context.Session.PlanMode = true;
        return Task.FromResult(new ToolExecutionResult(
            Name,
            true,
            "Plan mode ON — use read_file/grep/inspect_environment/tool_search only until exit_plan_mode.",
            Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>()));
    }
}

public sealed class ExitPlanModeTool : IAgentTool
{
    public string Name => "exit_plan_mode";
    public string Description => "Leave plan mode and allow edits. Input: {}";
    public bool IsReadOnly => false;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        context.Session.PlanMode = false;
        return Task.FromResult(new ToolExecutionResult(
            Name,
            true,
            "Plan mode OFF — write/edit/bash/run_build allowed.",
            Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>()));
    }
}
