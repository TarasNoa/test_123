namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed class DefaultSkillRegistry : ISkillRegistry
{
    private readonly Dictionary<string, SkillDefinition> _byId;

    public DefaultSkillRegistry()
    {
        var skills = new[]
        {
            new SkillDefinition(
                "libr4.plan.architect",
                "1.0.0",
                "Architecture planning",
                new[] { "planning", "architecture" },
                "trusted",
                new[] { "planning", "plan" },
                new SkillModelConfig(Temperature: 0.5, UseCascade: true),
                new SkillRunConfig(TimeoutSeconds: 600, MaxRetries: 3),
                new[] { "planner", "file-read", "file-write" }),
            new SkillDefinition(
                "libr4.generate.phased",
                "1.0.0",
                "Phased code generation",
                new[] { "generation", "csharp", "python" },
                "review-required",
                new[] { "generation", "generating" },
                new SkillModelConfig(Temperature: 0.7, MaxTokens: 8192),
                new SkillRunConfig(TimeoutSeconds: 900, MaxRetries: 2, RequiresIsolation: true),
                new[] { "codegen", "file-read", "file-write", "mcp-call" }),
            new SkillDefinition(
                "libr4.fix.dependency-aware",
                "1.0.0",
                "Cross-file fix iteration",
                new[] { "fix", "repair" },
                "review-required",
                new[] { "fixing", "fix" },
                new SkillModelConfig(Temperature: 0.6, MaxTokens: 4096),
                new SkillRunConfig(TimeoutSeconds: 600, MaxRetries: 3),
                new[] { "codegen", "file-read", "file-write", "diff-view" }),
            new SkillDefinition(
                "libr4.review.security",
                "1.0.0",
                "Security / secret scanning",
                new[] { "security", "governance" },
                "trusted",
                new[] { "review", "post_generation", "post_fix", "consistency" },
                new SkillModelConfig(Temperature: 0.3, MaxTokens: 2048),
                new SkillRunConfig(TimeoutSeconds: 300, MaxRetries: 1),
                new[] { "file-read", "regex-search", "credential-scan" }),
            new SkillDefinition(
                "libr4.context.pack",
                "1.0.0",
                "Compact context assembly",
                new[] { "context", "prompting" },
                "trusted",
                new[] { "planning", "generation", "generating", "consistency", "fixing", "fix" },
                new SkillModelConfig(Temperature: 0.0, MaxTokens: 1024),
                new SkillRunConfig(TimeoutSeconds: 120, MaxRetries: 1),
                new[] { "file-read", "memory-retrieve", "context-pack" }),
        };
        _byId = skills.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<SkillDefinition> List() => _byId.Values.ToList();

    public SkillDefinition? Find(string skillId) =>
        _byId.TryGetValue(skillId, out var s) ? s : null;
}
