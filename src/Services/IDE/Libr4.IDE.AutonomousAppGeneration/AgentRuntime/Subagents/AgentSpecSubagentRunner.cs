using Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting.Templates;
using Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;

public interface IAgentSpecSubagentRunner
{
    Task<AgentSessionResult> RunAsync(
        AgentSpec spec,
        string task,
        ToolContext parentContext,
        CancellationToken ct = default);
}

public sealed class AgentSpecSubagentRunner : IAgentSpecSubagentRunner
{
    private readonly IAgentBackendCoordinator _backends;
    private readonly IBuiltinPromptVarResolver _vars;
    private readonly IPromptTemplateRegistry? _templates;
    private readonly PromptVariantSelector? _variants;

    public AgentSpecSubagentRunner(
        IAgentBackendCoordinator backends,
        IBuiltinPromptVarResolver vars,
        IPromptTemplateRegistry? templates = null,
        PromptVariantSelector? variants = null)
    {
        _backends = backends;
        _vars = vars;
        _templates = templates;
        _variants = variants;
    }

    public Task<AgentSessionResult> RunAsync(
        AgentSpec spec,
        string task,
        ToolContext parentContext,
        CancellationToken ct = default)
    {
        var objective = BuildObjective(spec, task, parentContext);
        var mode = spec.IsReadOnly || spec.Name.Equals("computer", StringComparison.OrdinalIgnoreCase)
            ? AgentSessionMode.Repair
            : AgentSessionMode.Generation;

        var varContext = new BuiltinPromptVarContext
        {
            Plan = parentContext.Plan,
            WorkspaceHostPath = parentContext.Workspace.HostPath,
            BuildLog = parentContext.BuildLog,
            RunId = parentContext.Session.RunId,
            Stage = spec.Name.Equals("verify", StringComparison.OrdinalIgnoreCase)
                ? BuiltinPromptStage.Verify
                : spec.Name.Equals("computer", StringComparison.OrdinalIgnoreCase)
                    ? BuiltinPromptStage.Planning
                    : BuiltinPromptStage.Planning,
            ManifestFiles = parentContext.WorkingFiles.Select(f => f.RelativePath).Take(32).ToArray()
        };

        var runId = parentContext.Session.RunId ?? Guid.NewGuid();

        var request = new AgentSessionRunRequest(
            objective,
            parentContext.Workspace,
            parentContext.WorkingFiles,
            parentContext.Plan!,
            parentContext.Accessor,
            mode,
            parentContext.BuildLog,
            RunId: runId,
            MaxTurns: spec.MaxTurns,
            PromptStage: varContext.Stage,
            AllowedTools: spec.Toolset.Count == 0 ? null : spec.Toolset,
            SubagentRole: AgentModelRoleNames.Normalize(spec.Name),
            ModelOverride: spec.Model,
            TenantUserId: parentContext.Session.TenantUserId,
            SpaceId: parentContext.Session.SpaceId);

        var spawn = new AgentBackendSpawnRequest(
            runId,
            spec.Name,
            spec.Backend,
            request,
            objective);

        return _backends.RunSessionAsync(spawn, ct);
    }

    private string BuildObjective(AgentSpec spec, string task, ToolContext parentContext)
    {
        var objective = string.IsNullOrWhiteSpace(spec.Instruction)
            ? task
            : $"{spec.Instruction.Trim()}\n\nTASK:\n{task}";

        var varContext = new BuiltinPromptVarContext
        {
            Plan = parentContext.Plan,
            WorkspaceHostPath = parentContext.Workspace.HostPath,
            BuildLog = parentContext.BuildLog,
            RunId = parentContext.Session.RunId,
            Stage = BuiltinPromptStage.Planning,
            ManifestFiles = parentContext.WorkingFiles.Select(f => f.RelativePath).Take(32).ToArray()
        };
        objective = PromptVariableSubstitutor.Apply(objective, _vars, varContext);
        if (_templates is not null)
        {
            var variant = _variants?.SelectVariant(spec.Name, parentContext.Session.RunId);
            var rolePrompt = _templates.FormatRolePrompt(spec.Name, variant);
            if (!string.IsNullOrWhiteSpace(rolePrompt))
                objective = rolePrompt + "\n\n" + objective;
            objective = InstructionTemplateFormatter.Format(objective, AgentPromptBuilder.DefaultResponseHint);
        }

        return objective;
    }
}
