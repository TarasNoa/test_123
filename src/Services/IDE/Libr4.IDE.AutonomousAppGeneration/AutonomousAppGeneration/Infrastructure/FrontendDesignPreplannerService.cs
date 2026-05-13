using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

public sealed class FrontendDesignPreplannerService : IFrontendDesignPreplannerService
{
    private readonly IAIService _ai;
    private readonly IProviderCapabilityMatrix _providerMatrix;
    private readonly ILogger<FrontendDesignPreplannerService> _logger;
    private readonly string _artifactRoot = Path.Combine(Path.GetTempPath(), "libr4-frontend-design-artifacts");

    public FrontendDesignPreplannerService(
        IAIService ai,
        IProviderCapabilityMatrix providerMatrix,
        ILogger<FrontendDesignPreplannerService> logger)
    {
        _ai = ai;
        _providerMatrix = providerMatrix;
        _logger = logger;
        Directory.CreateDirectory(_artifactRoot);
    }

    public bool ShouldRunFor(GenerationPlan plan)
    {
        var frameworks = string.Join(' ', plan.TechStack.Frameworks).ToLowerInvariant();
        var languages = string.Join(' ', plan.TechStack.Languages).ToLowerInvariant();
        return frameworks.Contains("react", StringComparison.Ordinal)
               || frameworks.Contains("next", StringComparison.Ordinal)
               || frameworks.Contains("vue", StringComparison.Ordinal)
               || frameworks.Contains("angular", StringComparison.Ordinal)
               || frameworks.Contains("blazor", StringComparison.Ordinal)
               || (languages.Contains("typescript", StringComparison.Ordinal) && frameworks.Contains("api", StringComparison.Ordinal) == false);
    }

    public async Task<FrontendDesignPreplanResult?> GenerateDesignAsync(string userRequest, GenerationPlan plan, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(userRequest, plan);
        prompt = PromptPipelinePolicy.ApplyInputBudget("planning", prompt);

        var stageRequirement = _providerMatrix.GetStageRequirements("planning")
                               ?? new StageModelRequirement(
                                   Stage: "planning",
                                   RequiresFunctionCalling: false,
                                   RequiresStreaming: false,
                                   RequiresJsonMode: false,
                                   MinContextTokens: 32000,
                                   MinOutputTokens: 4096,
                                   MaxCostPer1kTokens: 0.01);
        var routing = _providerMatrix.RouteStage("planning", stageRequirement);

        try
        {
            var raw = await _ai.GenerateCompletionAsync(prompt, FrontendDesignSystemPrompt, routing.ModelId);
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var brief = raw.Length <= 4_000 ? raw : raw[..4_000];
            var artifact = BuildArtifact(plan);
            var export = ExportArtifact(artifact);
            return new FrontendDesignPreplanResult(brief, artifact, export);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Frontend design preplanner failed; continuing without design brief");
            return null;
        }
    }

    private FrontendDesignArtifact BuildArtifact(GenerationPlan plan)
    {
        var artifactId = $"ui-design-{Guid.NewGuid():N}";
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["spacing.base"] = "8px",
            ["radius.md"] = "10px",
            ["shadow.card"] = "0 2px 12px rgba(15,23,42,0.08)",
            ["motion.fast"] = "120ms ease-out"
        };
        var palette = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["brand.primary"] = "#2563EB",
            ["brand.accent"] = "#0EA5E9",
            ["bg.canvas"] = "#F8FAFC",
            ["text.primary"] = "#0F172A",
            ["status.error"] = "#DC2626"
        };
        var typography = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["font.family"] = "Inter, Segoe UI, sans-serif",
            ["text.h1"] = "700 32/40",
            ["text.h2"] = "600 24/32",
            ["text.body"] = "400 16/24"
        };
        var components = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["button"] = "variants=primary|secondary|ghost; states=hover|disabled|loading",
            ["input"] = "label+hint+error slots; focus ring required; invalid state explicit",
            ["card"] = "header/body/footer slots; elevation token=shadow.card",
            ["navigation"] = "topbar + sidebar patterns; active state required"
        };
        var screens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["dashboard"] = "summary cards + activity timeline + quick actions",
            ["list"] = "filter/search/sort + table/cards + bulk actions",
            ["details"] = "entity overview + tabs + audit panel",
            ["settings"] = "grouped preferences + save/discard pattern"
        };
        var accessibility = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["contrast"] = "AA minimum",
            ["keyboard"] = "all controls reachable with visible focus",
            ["aria"] = "labels for form controls and icon buttons",
            ["motion"] = "respect prefers-reduced-motion"
        };
        if (plan.TechStack.Frameworks.Any(f => f.Contains("blazor", StringComparison.OrdinalIgnoreCase)))
            components["navigation"] = "topbar + nav menu with collapsible sections; active state required";

        return new FrontendDesignArtifact(
            artifactId,
            "1.0",
            tokens,
            palette,
            typography,
            components,
            screens,
            accessibility);
    }

    private FrontendDesignArtifactExport ExportArtifact(FrontendDesignArtifact artifact)
    {
        var path = Path.Combine(_artifactRoot, $"{artifact.ArtifactId}.json");
        var json = JsonSerializer.Serialize(artifact, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json, Encoding.UTF8);
        var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        return new FrontendDesignArtifactExport(artifact.ArtifactId, path, sha, DateTime.UtcNow);
    }

    private static string BuildPrompt(string userRequest, GenerationPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"User request: {userRequest}");
        sb.AppendLine($"Application: {plan.ApplicationName}");
        sb.AppendLine($"Description: {plan.ApplicationDescription}");
        sb.AppendLine($"Tech stack: languages={string.Join(",", plan.TechStack.Languages)}; frameworks={string.Join(",", plan.TechStack.Frameworks)}");
        sb.AppendLine("Generate a practical frontend design brief that can guide code generation.");
        return sb.ToString();
    }

    private const string FrontendDesignSystemPrompt = """
You are a senior product designer.
Create a frontend design brief BEFORE implementation.
Output concise markdown with sections:
1) Design Goals
2) Information Architecture
3) Key Screens (with purpose)
4) Component System (buttons/forms/cards/navigation/feedback)
5) Visual Direction (color/typography/spacing)
6) Interaction Rules (states, validation, errors, loading)
7) Accessibility Checklist (WCAG-focused)
8) Handoff Notes for frontend code generator

Keep it implementation-friendly, specific, and not generic.
""";
}

