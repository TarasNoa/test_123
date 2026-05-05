using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class SkillRunner : ISkillRunner
{
    private readonly ISkillRegistry _registry;
    private readonly ISkillSelectionStrategy _selection;

    public SkillRunner(ISkillRegistry registry, ISkillSelectionStrategy selection)
    {
        _registry = registry;
        _selection = selection;
    }

    public Task RecordStageSelectionAsync(
        AppGenerationOrchestrator orchestrator,
        string stage,
        GenerationPlan? plan,
        CancellationToken ct)
    {
        _ = ct;
        var selection = _selection.SelectSkillsWithReasons(stage, plan);
        if (selection.Count == 0)
            return Task.CompletedTask;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        foreach (var (skillId, reason) in selection)
        {
            var def = _registry.Find(skillId);
            if (def is null) continue;

            // Build provenance profiles from schema-driven config
            var modelProfile = BuildModelProfile(def.ModelConfig);
            var toolProfile = BuildToolProfile(def.AllowedTools);
            var runtimeProfile = BuildRuntimeProfile(def.RunConfig);

            orchestrator.RecordSkillInvocation(new SkillInvocationAuditEntry(
                def.Id,
                def.Version,
                stage,
                def.SafetyLabel,
                DateTime.UtcNow,
                sw.ElapsedMilliseconds,
                "selected",
                string.Join(",", def.CapabilityTags),
                reason, // Selection reason (why this skill was chosen)
                modelProfile,
                toolProfile,
                runtimeProfile));
        }

        return Task.CompletedTask;
    }

    private static string BuildModelProfile(SkillModelConfig config)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(config.ModelHint))
            parts.Add($"model:{config.ModelHint}");
        parts.Add($"temp:{config.Temperature:F2}");
        parts.Add($"maxTokens:{config.MaxTokens}");
        if (config.UseCascade)
            parts.Add("cascade:true");
        return string.Join(", ", parts);
    }

    private static string BuildToolProfile(IReadOnlyList<string> allowedTools)
    {
        return $"tools:{allowedTools.Count}|{string.Join(",", allowedTools.Take(5))}{(allowedTools.Count > 5 ? ",..." : "")}";
    }

    private static string BuildRuntimeProfile(SkillRunConfig config)
    {
        var parts = new List<string>();
        parts.Add($"timeout:{config.TimeoutSeconds}s");
        parts.Add($"retries:{config.MaxRetries}");
        if (config.RequiresSandbox)
            parts.Add("sandbox:true");
        if (config.RequiresIsolation)
            parts.Add("isolation:true");
        return string.Join(", ", parts);
    }
}
