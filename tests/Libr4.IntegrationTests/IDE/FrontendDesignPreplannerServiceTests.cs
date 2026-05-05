using FluentAssertions;
using Libr4.AI.Infrastructure.AI;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class FrontendDesignPreplannerServiceTests
{
    [Fact]
    public void ShouldRunFor_ShouldBeTrue_ForFrontendFrameworks()
    {
        var service = new FrontendDesignPreplannerService(
            new FakeAiService("design"),
            new FakeProviderMatrix(),
            NullLogger<FrontendDesignPreplannerService>.Instance);

        var plan = BuildPlan(new[] { "TypeScript" }, new[] { "React" });

        service.ShouldRunFor(plan).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateDesignAsync_ShouldReturnResult_ForFrontendPlan()
    {
        var service = new FrontendDesignPreplannerService(
            new FakeAiService("## Design Goals\n- premium UI"),
            new FakeProviderMatrix(),
            NullLogger<FrontendDesignPreplannerService>.Instance);

        var plan = BuildPlan(new[] { "TypeScript" }, new[] { "React" });
        var result = await service.GenerateDesignAsync("Build modern dashboard", plan);

        result.Should().NotBeNull();
        result!.BriefMarkdown.Should().Contain("Design Goals");
        result.Artifact.Should().NotBeNull();
        result.Artifact.DesignTokens.Should().ContainKey("spacing.base");
        result.Artifact.Components.Should().ContainKey("button");
        result.Export.Should().NotBeNull();
        File.Exists(result.Export!.ArtifactPath).Should().BeTrue();
        result.Export.Sha256.Should().NotBeNullOrWhiteSpace();
    }

    private static GenerationPlan BuildPlan(IReadOnlyList<string> languages, IReadOnlyList<string> frameworks)
        => new(
            "App",
            "Desc",
            new TechStack(languages, frameworks, new[] { "PostgreSQL" }, new[] { "Docker" }, "rationale"),
            new[] { new GenerationPhase(1, "Scaffold", "desc", Array.Empty<AgentAssignment>()) },
            new[] { "planner" },
            "node:22-alpine",
            new[] { "npm ci" },
            new[] { "npm test" },
            5);

    private sealed class FakeAiService : IAIService
    {
        private readonly string _response;
        public FakeAiService(string response) => _response = response;
        public Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null) => Task.FromResult(_response);
        public Task<string> GenerateEmbeddingAsync(string text, string? model = null) => Task.FromResult("embedding");
        public Task<string> AnalyzeTextAsync(string text, string analysisType, string? model = null) => Task.FromResult("analysis");
        public Task<string> ChatAsync(string message, string? systemPrompt = null, string? model = null) => Task.FromResult("chat");
    }

    private sealed class FakeProviderMatrix : IProviderCapabilityMatrix
    {
        public IReadOnlyList<ProviderCapability> GetProviders() => new[]
        {
            new ProviderCapability("openrouter", "OpenRouter", false, false, true, true, 128000, 8192, 0.0)
        };

        public ProviderCapability? GetProvider(string providerId) => GetProviders().FirstOrDefault();

        public ModelRoutingDecision RouteStage(string stage, StageModelRequirement requirement)
            => new(stage, "openrouter", "openrouter/auto", "test");

        public StageModelRequirement? GetStageRequirements(string stage)
            => new(stage, false, false, true, 32000, 2048, 0.01);
    }
}

