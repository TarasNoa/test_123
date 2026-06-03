using Libr4.AI.Application.Abstractions;
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

    public GenericImplementerAgent(
        string skillPath,
        IAIService aiService,
        ILogger logger,
        IAgentSpawner? spawner = null) : base(skillPath)
    {
        _aiService = aiService;
        _logger = logger;
        _spawner = spawner;
    }

    public override async Task<AgentResult> ExecuteAsync(AgentContext context)
    {
        _logger.LogInformation(
            "Executing {SkillName} for application: {ApplicationName}",
            SkillName, context.ApplicationName);

        var skillInstructions = GetSkillInstructions();
        var delegatedContent = await ExecuteDelegatedSubtasksParallelAsync(context);
        var prompt = BuildGenerationPrompt(context, skillInstructions, delegatedContent);

        var response = await _aiService.GenerateCompletionAsync(prompt, skillInstructions);

        _logger.LogInformation(
            "{SkillName} completed. Output length: {Length} chars",
            SkillName, response?.Length ?? 0);

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

        return string.IsNullOrWhiteSpace(merged) ? null : merged;
    }

    private static string BuildGenerationPrompt(AgentContext context, string skillInstructions, string? delegatedArtifacts = null)
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

        if (context.GeneratedFiles != null && context.GeneratedFiles.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## EXISTING FILES");
            foreach (var file in context.GeneratedFiles.Take(20))
            {
                sb.AppendLine($"- {file.RelativePath}");
            }
        }

        if (!string.IsNullOrWhiteSpace(delegatedArtifacts))
        {
            sb.AppendLine();
            sb.AppendLine("## DELEGATED SUBAGENT OUTPUT (merge into final JSON files array)");
            sb.AppendLine(delegatedArtifacts);
        }

        sb.AppendLine();
        sb.AppendLine("## SKILL INSTRUCTIONS");
        sb.AppendLine(skillInstructions);

        sb.AppendLine();
        sb.AppendLine("## SUBAGENT DELEGATION");
        sb.AppendLine("If a task is too large or requires specialized expertise (e.g., authentication, database migrations, complex algorithms, UI animations),");
        sb.AppendLine("you MAY output a 'SUBTASKS:' section listing subtasks with their 'role' and 'description'.");
        sb.AppendLine("Available roles: auth-specialist, db-architect, api-designer, graphql-specialist, css-expert, a11y-specialist, k8s-specialist, qa-automation, tech-writer, etc.");
        sb.AppendLine("Do NOT delegate trivial tasks. Only delegate when the specialized skill genuinely improves output quality.");

        sb.AppendLine();
        sb.AppendLine("## OUTPUT (STRICT)");
        sb.AppendLine("Return ONLY valid JSON (no markdown prose): {\"files\":[{\"relativePath\":\"...\",\"content\":\"...\"}]}");
        sb.AppendLine("- Use repo-relative paths. For Java+React monorepos: backend/... and frontend/... (never root package.json or stray src/ tests only).");
        sb.AppendLine("- Include ALL files needed to build: backend/pom.xml, Spring main + controllers, frontend/package.json, vite config, App.tsx, API client.");
        sb.AppendLine("- Each file must be complete and compilable. No placeholders, no TODOs.");
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
