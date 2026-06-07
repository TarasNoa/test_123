using System.Text;
using System.Text.Json;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public interface IVerifyRecipeLlmDetector
{
    Task<VerifyRecipe?> TryDetectAsync(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan? plan,
        string? userRequest,
        CancellationToken ct = default);
}

public sealed class VerifyRecipeLlmDetector : IVerifyRecipeLlmDetector
{
    private const string DetectSystemPrompt = """
        You are verify-detect: classify a generated workspace into exactly one verify recipe id.
        Reply with JSON only: {"recipeId":"<id>","confidence":0.0-1.0,"reason":"..."}.
        Known recipe ids:
        calorie-vision, banking, django, fastapi, vite, solidjs, nextjs, spring-boot, dotnet, express, generic-fallback
        """;

    private readonly IAIService _ai;
    private readonly IReadOnlyDictionary<string, VerifyRecipe> _recipes;
    private readonly VerifySubagentOptions _options;
    private readonly ILogger<VerifyRecipeLlmDetector> _logger;

    public VerifyRecipeLlmDetector(
        IAIService ai,
        IReadOnlyDictionary<string, VerifyRecipe> recipes,
        IOptions<VerifySubagentOptions> options,
        ILogger<VerifyRecipeLlmDetector> logger)
    {
        _ai = ai;
        _recipes = recipes;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<VerifyRecipe?> TryDetectAsync(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan? plan,
        string? userRequest,
        CancellationToken ct = default)
    {
        if (!_options.EnableRecipeLlmFallback)
            return null;

        try
        {
            var prompt = BuildPrompt(files, plan, userRequest);
            var raw = await _ai.GenerateCompletionAsync(prompt, DetectSystemPrompt, model: null).ConfigureAwait(false);
            var doc = LlmJsonHelpers.ExtractJson(raw);
            if (doc is null)
            {
                _logger.LogWarning("[verify-detect] LLM reply did not contain JSON");
                return null;
            }

            using (doc)
            {
                var root = doc.RootElement;
                var recipeId = LlmJsonHelpers.GetString(root, "recipeId");
                if (string.IsNullOrWhiteSpace(recipeId))
                    return null;

                if (_recipes.TryGetValue(recipeId, out var recipe))
                    return recipe;

                _logger.LogWarning("[verify-detect] Unknown recipe id from LLM: {RecipeId}", recipeId);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[verify-detect] LLM fallback failed");
            return null;
        }
    }

    private static string BuildPrompt(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan? plan,
        string? userRequest)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Classify this workspace.");
        if (!string.IsNullOrWhiteSpace(userRequest))
            sb.AppendLine($"User request: {userRequest}");
        if (plan is not null)
        {
            sb.AppendLine($"Application: {plan.ApplicationName}");
            sb.AppendLine($"Languages: {string.Join(',', plan.TechStack.Languages)}");
            sb.AppendLine($"Frameworks: {string.Join(',', plan.TechStack.Frameworks)}");
            if (plan.BuildCommands.Count > 0)
                sb.AppendLine($"Build: {string.Join(" ; ", plan.BuildCommands)}");
            if (plan.TestCommands.Count > 0)
                sb.AppendLine($"Test: {string.Join(" ; ", plan.TestCommands)}");
        }

        sb.AppendLine("File tree (relative paths):");
        foreach (var path in files.Select(f => f.RelativePath.Replace('\\', '/')).Distinct().OrderBy(p => p).Take(80))
            sb.AppendLine($"- {path}");

        sb.AppendLine("Key markers:");
        AppendMarker(sb, files, "manage.py");
        AppendMarker(sb, files, "requirements.txt", token: "fastapi");
        AppendMarker(sb, files, "pom.xml", token: "spring-boot");
        AppendMarker(sb, files, "package.json", token: "solid-js");
        AppendMarker(sb, files, "package.json", token: "react");
        AppendMarker(sb, files, "package.json", token: "next");
        AppendMarker(sb, files, "vite.config");
        AppendMarker(sb, files, ".csproj");

        return sb.ToString();
    }

    private static void AppendMarker(StringBuilder sb, IReadOnlyList<GeneratedFile> files, string pathToken, string? token = null)
    {
        var hit = files.FirstOrDefault(f =>
            f.RelativePath.Contains(pathToken, StringComparison.OrdinalIgnoreCase) &&
            (token is null || f.Content.Contains(token, StringComparison.OrdinalIgnoreCase)));
        sb.AppendLine(hit is null ? $"- {pathToken}: no" : $"- {pathToken}: yes");
    }
}
