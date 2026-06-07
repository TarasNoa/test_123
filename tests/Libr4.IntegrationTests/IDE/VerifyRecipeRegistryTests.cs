using System.Text.Json;
using FluentAssertions;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class VerifyRecipeRegistryTests : IDisposable
{
    private readonly string _evidenceRoot;
    private readonly VerifyRecipeRegistry _registry;

    public VerifyRecipeRegistryTests()
    {
        _evidenceRoot = Path.Combine(Path.GetTempPath(), $"verify-recipe-{Guid.NewGuid():N}");
        _registry = CreateRegistry(llmRecipeId: null);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_evidenceRoot))
                Directory.Delete(_evidenceRoot, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public void AllRecipes_ContainsRequiredStacks()
    {
        var ids = _registry.AllRecipes.Select(r => r.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        ids.Should().Contain("django");
        ids.Should().Contain("fastapi");
        ids.Should().Contain("vite");
        ids.Should().Contain("solidjs");
        ids.Should().Contain("nextjs");
        ids.Should().Contain("spring-boot");
        ids.Should().Contain("dotnet");
        ids.Should().Contain("express");
        ids.Should().Contain("generic-fallback");
        ids.Should().Contain("calorie-vision");
        ids.Should().Contain("banking");
    }

    [Fact]
    public async Task Detect_DjangoFromManagePy()
    {
        var files = new List<GeneratedFile>
        {
            new("backend/manage.py", "python", "django"),
            new("backend/requirements.txt", "text", "Django>=5.0")
        };

        var result = await _registry.DetectAsync(new VerifyRecipeDetectionRequest(files));

        result.Recipe.Id.Should().Be("django");
        result.DetectionMethod.Should().Be("deterministic");
    }

    [Fact]
    public async Task Detect_CalorieVisionFromDjangoAndSolidJs()
    {
        var files = new List<GeneratedFile>
        {
            new("backend/manage.py", "python", "django"),
            new("frontend/package.json", "json", """{"dependencies":{"solid-js":"^1.8.0"}}""")
        };

        var result = await _registry.DetectAsync(new VerifyRecipeDetectionRequest(files));

        result.Recipe.Id.Should().Be("calorie-vision");
        result.Recipe.SmokeTargets.Should().HaveCount(2);
        result.Recipe.SmokeTargets[0].Port.Should().Be(8000);
        result.Recipe.SmokeTargets[1].Port.Should().Be(5173);
    }

    [Fact]
    public async Task Detect_BankingFromSpringAndReact()
    {
        var files = new List<GeneratedFile>
        {
            new("backend/pom.xml", "xml", "<artifactId>spring-boot-starter-web</artifactId>"),
            new("frontend/package.json", "json", """{"dependencies":{"react":"^18.0.0"}}""")
        };

        var result = await _registry.DetectAsync(new VerifyRecipeDetectionRequest(files));

        result.Recipe.Id.Should().Be("banking");
        result.Recipe.SmokeTargets[0].Port.Should().Be(8080);
        result.Recipe.SmokeTargets[1].Port.Should().Be(3000);
    }

    [Fact]
    public async Task Detect_PersistsManifestJson()
    {
        var runId = Guid.NewGuid();
        var files = new List<GeneratedFile>
        {
            new("backend/manage.py", "python", "django")
        };

        var result = await _registry.DetectAsync(new VerifyRecipeDetectionRequest(
            files,
            EvidenceRoot: _evidenceRoot,
            RunId: runId));

        result.ManifestPath.Should().NotBeNullOrWhiteSpace();
        File.Exists(result.ManifestPath!).Should().BeTrue();

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(result.ManifestPath!));
        doc.RootElement.GetProperty("recipeId").GetString().Should().Be("django");
        doc.RootElement.GetProperty("detectionMethod").GetString().Should().Be("deterministic");
        doc.RootElement.GetProperty("smokeTargets").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Detect_UsesLlmFallbackWhenDeterministicFails()
    {
        var registry = CreateRegistry(llmRecipeId: "nextjs");
        var files = new List<GeneratedFile>
        {
            new("README.md", "markdown", "mystery stack")
        };

        var result = await registry.DetectAsync(new VerifyRecipeDetectionRequest(files));

        result.Recipe.Id.Should().Be("nextjs");
        result.DetectionMethod.Should().Be("verify-detect-llm");
    }

    private VerifyRecipeRegistry CreateRegistry(string? llmRecipeId)
    {
        var ai = new Mock<IAIService>();
        if (llmRecipeId is not null)
        {
            ai.Setup(a => a.GenerateCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync($$"""{"recipeId":"{{llmRecipeId}}","confidence":0.9,"reason":"test"}""");
        }

        var recipes = VerifyRecipeCatalog.BuildAll().ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
        var llm = new VerifyRecipeLlmDetector(
            ai.Object,
            recipes,
            Options.Create(new VerifySubagentOptions { EnableRecipeLlmFallback = llmRecipeId is not null }),
            NullLogger<VerifyRecipeLlmDetector>.Instance);

        return new VerifyRecipeRegistry(
            llm,
            Options.Create(new VerifySubagentOptions { EnableRecipeLlmFallback = llmRecipeId is not null }),
            NullLogger<VerifyRecipeRegistry>.Instance);
    }
}
