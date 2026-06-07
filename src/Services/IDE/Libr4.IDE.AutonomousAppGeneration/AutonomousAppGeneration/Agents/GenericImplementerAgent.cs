using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Generic implementer agent that can work with any SKILL.md file.
/// Used as the base for all code-generation agents across different tech stacks.
/// </summary>
public sealed class GenericImplementerAgent : AgentSkillBase
{
    private readonly IAIService _aiService;
    private readonly ILogger _logger;
    private readonly IAgentSpawner? _spawner;
    private readonly IProviderCapabilityMatrix? _providerMatrix;

    public GenericImplementerAgent(
        string skillPath,
        IAIService aiService,
        ILogger logger,
        IAgentSpawner? spawner = null,
        IProviderCapabilityMatrix? providerMatrix = null) : base(skillPath)
    {
        _aiService = aiService;
        _logger = logger;
        _spawner = spawner;
        _providerMatrix = providerMatrix;
    }

    public override async Task<AgentResult> ExecuteAsync(AgentContext context)
    {
        _logger.LogInformation(
            "Executing {SkillName} for application: {ApplicationName}",
            SkillName, context.ApplicationName);

        var skillInstructions = GetSkillInstructions();
        var scoped = context.ScopedOutputOnly || context.TargetRelativePaths.Length > 0;
        var delegatedContent = scoped ? null : await ExecuteDelegatedSubtasksParallelAsync(context);
        var prompt = BuildGenerationPrompt(context, delegatedContent);

        var modelId = ResolveGenerationModelId();
        var response = await _aiService.GenerateCompletionAsync(prompt, skillInstructions, modelId);

        _logger.LogInformation(
            "{SkillName} completed. Output length: {Length} chars, model={Model}",
            SkillName, response?.Length ?? 0, modelId ?? "(default)");

        var suggestedSubtasks = ExtractSuggestedSubtasks(response);
        if (suggestedSubtasks.Count > 0)
        {
            _logger.LogInformation(
                "{SkillName} suggested {Count} subtasks for delegation",
                SkillName, suggestedSubtasks.Count);
        }

        return new AgentResult
        {
            IsSuccess = !string.IsNullOrWhiteSpace(response),
            Content = response ?? string.Empty,
            SuggestedSubtasks = suggestedSubtasks
        };
    }

    /// <summary>
    /// Spawn a subagent for a specialized subtask and execute it.
    /// </summary>
    public async Task<AgentResult?> SpawnSubagentAsync(string role, AgentContext subContext, CancellationToken ct = default)
    {
        if (_spawner is null)
        {
            _logger.LogWarning("Cannot spawn subagent: no IAgentSpawner configured");
            return null;
        }

        _logger.LogInformation("Delegating to subagent role='{Role}' for task '{Description}'", role, subContext.Description);
        return await _spawner.SpawnAndExecuteAsync(role, subContext, ct);
    }

    private string? ResolveGenerationModelId()
    {
        if (_providerMatrix is null)
            return null;

        var requirement = _providerMatrix.GetStageRequirements("generation")
                          ?? new StageModelRequirement(
                              Stage: "generation",
                              RequiresFunctionCalling: false,
                              RequiresStreaming: false,
                              RequiresJsonMode: false,
                              MinContextTokens: 8000,
                              MinOutputTokens: 8192,
                              MaxCostPer1kTokens: 0.01);

        return _providerMatrix.RouteStage("generation", requirement).ModelId;
    }

    private async Task<string?> ExecuteDelegatedSubtasksParallelAsync(AgentContext context)
    {
        var task = context.Task;
        if (_spawner is null || task is null || task.Subtasks.Count == 0)
            return null;

        _logger.LogInformation(
            "{SkillName}: delegating {Count} subtasks in parallel via spawner",
            SkillName,
            task.Subtasks.Count);

        var results = await Task.WhenAll(task.Subtasks.Select(async sub =>
        {
            var role = string.IsNullOrWhiteSpace(sub.Context.TechStack)
                ? "api-designer"
                : sub.Context.TechStack;
            sub.Context.ApplicationName = string.IsNullOrWhiteSpace(sub.Context.ApplicationName)
                ? context.ApplicationName
                : sub.Context.ApplicationName;
            if (string.IsNullOrWhiteSpace(sub.Context.Description))
                sub.Context.Description = sub.Description;

            try
            {
                return await _spawner.SpawnAndExecuteAsync(role, sub.Context);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Subagent role {Role} failed", role);
                return null;
            }
        }));

        var merged = string.Join(
            "\n",
            results.Where(r => r is not null && !string.IsNullOrWhiteSpace(r.Content)).Select(r => r!.Content));

        task.Subtasks.Clear();
        if (context.Task?.Subtasks.Count > 0 == true)
            context.Task.Subtasks.Clear();

        return string.IsNullOrWhiteSpace(merged) ? null : merged;
    }

    private static string BuildGenerationPrompt(AgentContext context, string? delegatedArtifacts = null)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## TASK");
        sb.AppendLine();
        sb.AppendLine($"Application: {context.ApplicationName}");
        sb.AppendLine($"Description: {context.Description}");
        sb.AppendLine($"Tech Stack: {context.TechStack}");

        if (!string.IsNullOrWhiteSpace(context.Feedback))
        {
            sb.AppendLine();
            sb.AppendLine("## FEEDBACK / FIX INSTRUCTIONS");
            sb.AppendLine(context.Feedback);
        }

        var scoped = context.ScopedOutputOnly || context.TargetRelativePaths.Length > 0;
        if (context.GeneratedFiles != null && context.GeneratedFiles.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## EXISTING WORKSPACE (already generated — stay consistent)");
            foreach (var file in context.GeneratedFiles)
            {
                sb.AppendLine($"### {file.RelativePath}");
                if (!string.IsNullOrWhiteSpace(file.Content))
                    sb.AppendLine(file.Content);
                else
                    sb.AppendLine("(empty stub)");
                sb.AppendLine();
            }
        }

        if (!string.IsNullOrWhiteSpace(delegatedArtifacts))
        {
            sb.AppendLine();
            sb.AppendLine("## DELEGATED SUBAGENT OUTPUT (merge into final JSON files array)");
            sb.AppendLine(delegatedArtifacts);
        }

        if (!scoped)
        {
            sb.AppendLine();
            sb.AppendLine("## SUBAGENT DELEGATION");
            sb.AppendLine("If a task is too large or requires specialized expertise (e.g., authentication, database migrations, complex algorithms, UI animations),");
            sb.AppendLine("you MAY output a 'SUBTASKS:' section listing subtasks with their 'role' and 'description'.");
            sb.AppendLine("Available roles: auth-specialist, db-architect, api-designer, graphql-specialist, css-expert, a11y-specialist, k8s-specialist, qa-automation, tech-writer, etc.");
            sb.AppendLine("Do NOT delegate trivial tasks. Only delegate when the specialized skill genuinely improves output quality.");
        }

        sb.AppendLine();
        sb.AppendLine("## OUTPUT (STRICT)");
        sb.AppendLine("Return ONLY valid JSON (no markdown prose): {\"files\":[{\"relativePath\":\"...\",\"content\":\"...\"}]}");
        sb.AppendLine("- Use repo-relative paths. For Java+React monorepos: backend/... and frontend/... (never root package.json or stray src/ tests only).");
        if (context.PlannedPhasePaths.Length > 0)
        {
            sb.AppendLine("## PLANNED PATHS IN THIS PHASE (reference only — do not emit unless assigned as TARGET)");
            foreach (var path in context.PlannedPhasePaths.Take(40))
                sb.AppendLine($"- {path}");
            if (context.PlannedPhasePaths.Length > 40)
                sb.AppendLine($"... +{context.PlannedPhasePaths.Length - 40} more planned paths");
            sb.AppendLine("- Do NOT invent paths outside this plan. Do NOT duplicate TARGET in multiple files.");
        }

        if (scoped && context.TargetRelativePaths.Length > 0)
        {
            sb.AppendLine("## INCREMENTAL MODE");
            sb.AppendLine("- Some targets may already exist in EXISTING WORKSPACE (from seed or prior tasks).");
            sb.AppendLine("- Return {\"files\":[]} ONLY when every TARGET is fully complete, consistent, and production-ready for this task.");
            sb.AppendLine("- If a target is missing, stubbed, or incomplete — you MUST emit that path with the full file body.");
            sb.AppendLine("- Config, security, controllers, tests, and DevOps targets: never skip — emit complete files.");
            sb.AppendLine("- If it needs fixes or missing pieces — return ONLY that path with the full updated file body.");
            sb.AppendLine("- Return ONLY these paths when emitting files (exactly one entry per path):");
            foreach (var path in context.TargetRelativePaths)
                sb.AppendLine($"  - {path}");
            sb.AppendLine("- Do NOT emit any other relativePath. Do NOT output SUBTASKS.");
        }
        else
        {
            sb.AppendLine("- Emit only files required for THIS task scope (not the entire repository).");
            sb.AppendLine("- Each file must be complete and compilable. No placeholders, no TODOs.");
        }

        sb.AppendLine("- Do NOT embed file content inside relativePath; paths must be a single line.");

        return sb.ToString();
    }

    private List<AgentTask> ExtractSuggestedSubtasks(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return new List<AgentTask>();

        var subtasks = new List<AgentTask>();
        var subtaskSectionIndex = response.IndexOf("SUBTASKS:", StringComparison.OrdinalIgnoreCase);
        if (subtaskSectionIndex < 0)
            return subtasks;

        var section = response.Substring(subtaskSectionIndex);
        var lines = section.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("##") || trimmed.StartsWith("{"))
                break;

            // Parse format: "- role: description" or "1. role: description"
            var clean = trimmed.TrimStart('-', '*', ' ', '\t', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.');
            var colonIndex = clean.IndexOf(':');
            if (colonIndex > 0)
            {
                var role = clean.Substring(0, colonIndex).Trim();
                var desc = clean.Substring(colonIndex + 1).Trim();
                subtasks.Add(new AgentTask
                {
                    Description = desc,
                    Context = new AgentContext { Description = desc, TechStack = role }
                });
            }
        }

        return subtasks;
    }
}
