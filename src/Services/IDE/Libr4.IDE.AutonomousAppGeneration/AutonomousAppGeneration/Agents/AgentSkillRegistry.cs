using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// Maps technology stacks and agent roles to SKILL.md file paths.
/// Supports 30+ languages and frameworks with proper fallback hierarchy.
/// </summary>
public sealed class AgentSkillRegistry
{
    private readonly ILogger<AgentSkillRegistry> _logger;
    private readonly string _baseDir;

    // Primary key: stack identifier (e.g., "csharp-aspnet", "python-django")
    // Value: relative path under Agents/Skills/
    private readonly Dictionary<string, string> _skillPaths;

    // Fallback chains: if exact match not found, try these in order
    private readonly Dictionary<string, string[]> _fallbackChains;

    public AgentSkillRegistry(ILogger<AgentSkillRegistry> logger, string? baseDirectory = null)
    {
        _logger = logger;
        _baseDir = baseDirectory ?? Path.Combine(AppContext.BaseDirectory, "Agents", "Skills");
        _skillPaths = BuildSkillPaths();
        _fallbackChains = BuildFallbackChains();
    }

    /// <summary>
    /// Get the skill directory for a given stack and phase.
    /// </summary>
    public string GetSkillPath(string stackId, AgentPhase phase = AgentPhase.Backend)
    {
        var key = BuildKey(stackId, phase);

        if (_skillPaths.TryGetValue(key, out var path))
            return NormalizePath(path);

        // Try fallbacks
        if (_fallbackChains.TryGetValue(stackId, out var fallbacks))
        {
            foreach (var fb in fallbacks)
            {
                var fbKey = BuildKey(fb, phase);
                if (_skillPaths.TryGetValue(fbKey, out var fbPath))
                    return NormalizePath(fbPath);
            }
        }

        // Ultimate fallback: generic code-generation
        var genericPath = phase switch
        {
            AgentPhase.ReviewSpec => "spec-compliance-reviewer/SKILL.md",
            AgentPhase.ReviewQuality => "code-review/SKILL.md",
            _ => "code-generation/SKILL.md"
        };

        _logger.LogWarning(
            "No exact skill match for stack '{StackId}' phase '{Phase}'. Falling back to generic.",
            stackId, phase);

        return NormalizePath(genericPath);
    }

    /// <summary>
    /// Resolve a tech stack description (from LLM planner) to a canonical stack ID.
    /// </summary>
    public string ResolveStackId(string techStackDescription)
    {
        var normalized = techStackDescription.ToLowerInvariant().Replace(" ", "-").Replace(".", "");

        // Direct matches
        foreach (var key in _skillPaths.Keys.Select(k => k.Split("-")[0]).Distinct())
        {
            if (normalized.Contains(key))
                return key;
        }

        // Language detection
        var languages = new[]
        {
            ("csharp", new[] { "c#", "dotnet", "asp.net", "blazor", ".net" }),
            ("javascript", new[] { "js", "node", "express", "nestjs", "fastify" }),
            ("typescript", new[] { "ts", "react", "vue", "angular", "svelte", "solidjs", "nextjs", "nuxt" }),
            ("python", new[] { "py", "django", "fastapi", "flask", "fast-api" }),
            ("java", new[] { "spring", "springboot", "quarkus", "jakarta" }),
            ("go", new[] { "golang", "gin", "echo", "fiber" }),
            ("rust", new[] { "axum", "actix", "tokio", "rocket" }),
            ("php", new[] { "laravel", "symfony", "lumen" }),
            ("ruby", new[] { "rails", "sinatra", "rack" }),
            ("kotlin", new[] { "ktor", "android" }),
            ("scala", new[] { "play", "akka" }),
            ("swift", new[] { "ios", "vapor" }),
            ("dart", new[] { "flutter" }),
            ("elixir", new[] { "phoenix" }),
            ("clojure", new[] { "luminus" }),
            ("ocaml", new[] { "dream" }),
        };

        foreach (var (langId, aliases) in languages)
        {
            if (aliases.Any(alias => normalized.Contains(alias)))
            {
                _logger.LogInformation("Resolved stack '{Description}' to language '{LangId}'", techStackDescription, langId);
                return langId;
            }
        }

        _logger.LogWarning("Could not resolve stack '{Description}'. Using 'csharp' as ultimate fallback.", techStackDescription);
        return "csharp";
    }

    public IReadOnlyCollection<string> SupportedStacks => _skillPaths.Keys
        .Select(k => k.Split("-")[0])
        .Distinct()
        .ToList();

    private static string BuildKey(string stackId, AgentPhase phase)
    {
        var phaseSuffix = phase switch
        {
            AgentPhase.Backend => "backend",
            AgentPhase.Frontend => "frontend",
            AgentPhase.Database => "database",
            AgentPhase.DevOps => "devops",
            AgentPhase.Documentation => "documentation",
            AgentPhase.Observability => "observability",
            AgentPhase.CICD => "cicd",
            AgentPhase.ReviewSpec => "spec-review",
            AgentPhase.ReviewQuality => "quality-review",
            _ => "generic"
        };
        return $"{stackId}-{phaseSuffix}";
    }

    private string NormalizePath(string relativePath)
    {
        return Path.Combine(_baseDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private Dictionary<string, string> BuildSkillPaths()
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // C# Ecosystem
        paths["csharp-backend"] = "csharp-aspnet/SKILL.md";
        paths["csharp-frontend"] = "csharp-blazor/SKILL.md";

        // JavaScript Ecosystem
        paths["javascript-backend"] = "js-express/SKILL.md";
        paths["javascript-frontend"] = "js-react/SKILL.md";

        // TypeScript Ecosystem
        paths["typescript-backend"] = "ts-nestjs/SKILL.md";
        paths["typescript-frontend"] = "ts-react/SKILL.md";
        paths["solidjs-frontend"] = "ts-solidjs/SKILL.md";

        // Python Ecosystem
        paths["python-backend"] = "python-django/SKILL.md";
        paths["python-fastapi"] = "python-fastapi/SKILL.md";
        paths["python-ml"] = "python-ml/SKILL.md";

        // Java Ecosystem
        paths["java-backend"] = "java-spring/SKILL.md";
        paths["java-frontend"] = "java-vaadin/SKILL.md";

        // Go Ecosystem
        paths["go-backend"] = "go-gin/SKILL.md";

        // Rust Ecosystem
        paths["rust-backend"] = "rust-axum/SKILL.md";

        // PHP Ecosystem
        paths["php-backend"] = "php-laravel/SKILL.md";

        // Ruby Ecosystem
        paths["ruby-backend"] = "ruby-rails/SKILL.md";

        // Kotlin Ecosystem
        paths["kotlin-backend"] = "kotlin-ktor/SKILL.md";

        // Scala Ecosystem
        paths["scala-backend"] = "scala-play/SKILL.md";

        // Swift Ecosystem
        paths["swift-backend"] = "swift-vapor/SKILL.md";
        paths["swift-mobile"] = "swift-ios/SKILL.md";

        // Dart / Flutter
        paths["dart-mobile"] = "dart-flutter/SKILL.md";

        // Mobile Cross-platform
        paths["javascript-mobile"] = "js-reactnative/SKILL.md";

        // DevOps / Infrastructure
        paths["generic-devops"] = "devops-engineer/SKILL.md";
        paths["generic-cicd"] = "ci-cd-pipeline-builder/SKILL.md";
        paths["generic-observability"] = "observability-designer/SKILL.md";

        // Database & Data
        paths["generic-database"] = "database-designer/SKILL.md";

        // Documentation
        paths["generic-documentation"] = "documentation-writer/SKILL.md";

        // Reviewers (stack-agnostic)
        paths["generic-spec-review"] = "spec-compliance-reviewer/SKILL.md";
        paths["generic-quality-review"] = "code-review/SKILL.md";

        return paths;
    }

    private Dictionary<string, string[]> BuildFallbackChains()
    {
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // C# fallbacks
            ["csharp"] = new[] { "csharp" },
            ["dotnet"] = new[] { "csharp" },
            ["asp.net"] = new[] { "csharp" },
            ["blazor"] = new[] { "csharp" },

            // JS fallbacks
            ["javascript"] = new[] { "javascript", "typescript" },
            ["node"] = new[] { "javascript", "typescript" },
            ["express"] = new[] { "javascript" },
            ["nestjs"] = new[] { "typescript", "javascript" },
            ["fastify"] = new[] { "javascript", "typescript" },

            // TS fallbacks
            ["typescript"] = new[] { "typescript", "javascript" },
            ["react"] = new[] { "typescript", "javascript" },
            ["vue"] = new[] { "typescript", "javascript" },
            ["angular"] = new[] { "typescript", "javascript" },
            ["svelte"] = new[] { "typescript", "javascript" },
            ["solidjs"] = new[] { "typescript", "javascript" },
            ["nextjs"] = new[] { "typescript", "javascript" },
            ["nuxt"] = new[] { "typescript", "javascript" },

            // Python fallbacks
            ["python"] = new[] { "python" },
            ["django"] = new[] { "python" },
            ["fastapi"] = new[] { "python", "python" },
            ["flask"] = new[] { "python" },
            ["pytorch"] = new[] { "python-ml", "python" },
            ["tensorflow"] = new[] { "python-ml", "python" },
            ["langchain"] = new[] { "python-ml", "python" },

            // Java fallbacks
            ["java"] = new[] { "java" },
            ["spring"] = new[] { "java" },
            ["springboot"] = new[] { "java" },
            ["quarkus"] = new[] { "java" },
            ["jakarta"] = new[] { "java" },

            // Go fallbacks
            ["go"] = new[] { "go", "golang" },
            ["golang"] = new[] { "go", "golang" },
            ["gin"] = new[] { "go" },
            ["echo"] = new[] { "go" },
            ["fiber"] = new[] { "go" },

            // Rust fallbacks
            ["rust"] = new[] { "rust" },
            ["axum"] = new[] { "rust" },
            ["actix"] = new[] { "rust" },
            ["rocket"] = new[] { "rust" },

            // PHP fallbacks
            ["php"] = new[] { "php" },
            ["laravel"] = new[] { "php" },
            ["symfony"] = new[] { "php" },

            // Ruby fallbacks
            ["ruby"] = new[] { "ruby" },
            ["rails"] = new[] { "ruby" },

            // Kotlin fallbacks
            ["kotlin"] = new[] { "kotlin" },
            ["ktor"] = new[] { "kotlin" },

            // Scala fallbacks
            ["scala"] = new[] { "scala" },
            ["play"] = new[] { "scala" },

            // Swift fallbacks
            ["swift"] = new[] { "swift" },
            ["vapor"] = new[] { "swift" },
            ["ios"] = new[] { "swift" },

            // Dart fallbacks
            ["dart"] = new[] { "dart" },
            ["flutter"] = new[] { "dart" },

            // Mobile cross-platform
            ["reactnative"] = new[] { "javascript-mobile", "javascript" },
        };
    }
}

public enum AgentPhase
{
    Backend,
    Frontend,
    Database,
    DevOps,
    Documentation,
    Observability,
    CICD,
    ReviewSpec,
    ReviewQuality,
    Generic
}
