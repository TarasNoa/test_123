using System.Text.Json;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public sealed class VerifyRecipeRegistry : IVerifyRecipeRegistry
{
    private readonly IVerifyRecipeLlmDetector _llmDetector;
    private readonly VerifySubagentOptions _options;
    private readonly ILogger<VerifyRecipeRegistry> _logger;
    private readonly IReadOnlyDictionary<string, VerifyRecipe> _byId;

    public VerifyRecipeRegistry(
        IVerifyRecipeLlmDetector llmDetector,
        IOptions<VerifySubagentOptions> options,
        ILogger<VerifyRecipeRegistry> logger)
    {
        _llmDetector = llmDetector;
        _options = options.Value;
        _logger = logger;
        AllRecipes = VerifyRecipeCatalog.BuildAll();
        _byId = AllRecipes.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<VerifyRecipe> AllRecipes { get; }

    public VerifyRecipe? TryGet(string recipeId) =>
        _byId.TryGetValue(recipeId, out var recipe) ? recipe : null;

    public async Task<VerifyRecipeDetectionResult> DetectAsync(
        VerifyRecipeDetectionRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var files = request.Files ?? Array.Empty<GeneratedFile>();
        var (recipe, method) = DetectDeterministic(files, request.Plan, request.UserRequest);

        if (recipe.Id == "generic-fallback")
        {
            var llmRecipe = await _llmDetector.TryDetectAsync(files, request.Plan, request.UserRequest, ct)
                .ConfigureAwait(false);
            if (llmRecipe is not null)
            {
                recipe = llmRecipe;
                method = "verify-detect-llm";
            }
            else if (TryBuildFromPlan(request.Plan) is { } planRecipe)
            {
                recipe = planRecipe;
                method = "plan-fallback";
            }
        }

        string? manifestPath = null;
        if (request.RunId is Guid runId && !string.IsNullOrWhiteSpace(request.EvidenceRoot))
        {
            var evidenceDir = Path.Combine(request.EvidenceRoot, runId.ToString("D"), "verify");
            Directory.CreateDirectory(evidenceDir);
            manifestPath = Path.Combine(evidenceDir, "manifest.json");
            await PersistManifestAsync(recipe, method, files, request.Plan, manifestPath, ct).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "[VerifyRecipe] Detected {RecipeId} via {Method}",
            recipe.Id,
            method);

        return new VerifyRecipeDetectionResult(recipe, method, manifestPath);
    }

    private (VerifyRecipe Recipe, string Method) DetectDeterministic(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan? plan,
        string? userRequest)
    {
        var paths = files.Select(f => f.RelativePath.Replace('\\', '/')).ToList();
        var blob = BuildBlob(paths, plan, userRequest);

        if (HasPath(paths, "manage.py") && (HasSolidJs(files) || blob.Contains("solidjs", StringComparison.OrdinalIgnoreCase)))
            return (Require("calorie-vision"), "deterministic");

        if ((HasPath(paths, "pom.xml") || HasSpringBoot(files)) &&
            (HasReact(files) || blob.Contains("banking", StringComparison.OrdinalIgnoreCase)))
            return (Require("banking"), "deterministic");

        if (HasPath(paths, "manage.py") || ContentContains(files, "django"))
            return (Require("django"), "deterministic");

        if (HasFastApi(files) || blob.Contains("fastapi", StringComparison.OrdinalIgnoreCase))
            return (Require("fastapi"), "deterministic");

        if (HasPath(paths, "next.config") || blob.Contains("next.js", StringComparison.OrdinalIgnoreCase) || blob.Contains("nextjs", StringComparison.OrdinalIgnoreCase))
            return (Require("nextjs"), "deterministic");

        if (HasSolidJs(files))
            return (Require("solidjs"), "deterministic");

        if (HasPath(paths, "vite.config"))
            return (Require("vite"), "deterministic");

        if (HasSpringBoot(files) || HasPath(paths, "pom.xml") || HasPath(paths, "build.gradle"))
            return (Require("spring-boot"), "deterministic");

        if (paths.Any(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
            return (Require("dotnet"), "deterministic");

        if (HasExpress(files) || blob.Contains("express", StringComparison.OrdinalIgnoreCase))
            return (Require("express"), "deterministic");

        return (Require("generic-fallback"), "deterministic");
    }

    private VerifyRecipe Require(string id) => _byId[id];

    private static VerifyRecipe? TryBuildFromPlan(GenerationPlan? plan)
    {
        if (plan is null)
            return null;

        var smokeKind = plan.TechStack.Frameworks.Any(f =>
            f.Contains("react", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("solid", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("next", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("vue", StringComparison.OrdinalIgnoreCase))
            ? VerifySmokeKind.Browser
            : VerifySmokeKind.Http;

        return new VerifyRecipe(
            Id: "generic-fallback",
            DisplayName: $"Plan-derived ({plan.ApplicationName})",
            InstallCommands: [],
            BuildCommands: plan.BuildCommands.ToList(),
            TestCommands: plan.TestCommands.ToList(),
            StartCommands: [],
            SmokeTargets: [],
            SmokeKind: smokeKind);
    }

    private static async Task PersistManifestAsync(
        VerifyRecipe recipe,
        string detectionMethod,
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan? plan,
        string manifestPath,
        CancellationToken ct)
    {
        var payload = new
        {
            recipeId = recipe.Id,
            displayName = recipe.DisplayName,
            detectionMethod,
            detectedAtUtc = DateTime.UtcNow,
            fileCount = files.Count,
            plan = plan is null
                ? null
                : new
                {
                    plan.ApplicationName,
                    languages = plan.TechStack.Languages,
                    frameworks = plan.TechStack.Frameworks
                },
            installCommands = recipe.InstallCommands,
            buildCommands = recipe.BuildCommands,
            testCommands = recipe.TestCommands,
            startCommands = recipe.StartCommands,
            smokeKind = recipe.SmokeKind.ToString(),
            smokeTargets = recipe.SmokeTargets.Select(t => new
            {
                t.Name,
                t.Url,
                t.Port,
                kind = t.Kind.ToString()
            })
        };

        await File.WriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }),
            ct).ConfigureAwait(false);
    }

    private static string BuildBlob(IReadOnlyList<string> paths, GenerationPlan? plan, string? userRequest)
    {
        return string.Join(' ',
            new[]
            {
                userRequest ?? string.Empty,
                plan?.ApplicationName ?? string.Empty,
                plan?.ApplicationDescription ?? string.Empty,
                plan is null ? string.Empty : string.Join(' ', plan.TechStack.Languages),
                plan is null ? string.Empty : string.Join(' ', plan.TechStack.Frameworks),
                string.Join(' ', paths.Take(40))
            }).ToLowerInvariant();
    }

    private static bool HasPath(IReadOnlyList<string> paths, string token) =>
        paths.Any(p => p.Contains(token, StringComparison.OrdinalIgnoreCase));

    private static bool HasSolidJs(IReadOnlyList<GeneratedFile> files) =>
        files.Any(f =>
            f.RelativePath.Contains("package.json", StringComparison.OrdinalIgnoreCase) &&
            f.Content.Contains("solid-js", StringComparison.OrdinalIgnoreCase));

    private static bool HasReact(IReadOnlyList<GeneratedFile> files) =>
        files.Any(f =>
            f.RelativePath.Contains("package.json", StringComparison.OrdinalIgnoreCase) &&
            (f.Content.Contains("\"react\"", StringComparison.OrdinalIgnoreCase) ||
             f.Content.Contains("'react'", StringComparison.OrdinalIgnoreCase)));

    private static bool HasFastApi(IReadOnlyList<GeneratedFile> files) =>
        files.Any(f =>
            (f.RelativePath.Contains("requirements", StringComparison.OrdinalIgnoreCase) ||
             f.RelativePath.EndsWith(".py", StringComparison.OrdinalIgnoreCase)) &&
            f.Content.Contains("fastapi", StringComparison.OrdinalIgnoreCase));

    private static bool HasSpringBoot(IReadOnlyList<GeneratedFile> files) =>
        files.Any(f =>
            (f.RelativePath.Contains("pom.xml", StringComparison.OrdinalIgnoreCase) ||
             f.RelativePath.Contains("build.gradle", StringComparison.OrdinalIgnoreCase)) &&
            f.Content.Contains("spring-boot", StringComparison.OrdinalIgnoreCase));

    private static bool HasExpress(IReadOnlyList<GeneratedFile> files) =>
        files.Any(f =>
            f.RelativePath.Contains("package.json", StringComparison.OrdinalIgnoreCase) &&
            f.Content.Contains("express", StringComparison.OrdinalIgnoreCase));

    private static bool ContentContains(IReadOnlyList<GeneratedFile> files, string token) =>
        files.Any(f => f.Content.Contains(token, StringComparison.OrdinalIgnoreCase));
}
