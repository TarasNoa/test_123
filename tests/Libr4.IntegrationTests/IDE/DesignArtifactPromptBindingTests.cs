using FluentAssertions;
using Libr4.AI.Infrastructure.AI;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class DesignArtifactPromptBindingTests
{
    [Fact]
    public async Task GenerateInitialAsync_ShouldEmbedArtifactBindingIntoPrompt()
    {
        var ai = new RecordingAiService("""
        {"files":[{"relativePath":"src/App/Program.cs","content":"var x = 1;"}]}
        """);
        var service = new LlmCodeGenerationService(
            ai,
            NullLogger<LlmCodeGenerationService>.Instance,
            Options.Create(new AutonomousGenerationOptions
            {
                InitialBatchSize = 1,
                MaxBatchAttempts = 1,
                MaxManifestFiles = 5,
                LlmStepTimeoutSeconds = 30
            }),
            new FakeProviderMatrix());

        var description = """
        Frontend app.
        [[UI_DESIGN_ARTIFACT_ID:ui-design-abc]]
        [[UI_DESIGN_ARTIFACT_JSON_BEGIN]]
        {"artifactId":"ui-design-abc","version":"1.0","designTokens":{"spacing.base":"8px"},"palette":{"brand.primary":"#2563EB"},"typography":{"text.h1":"700 32/40"},"components":{"button":"variants"},"screens":{"dashboard":"summary"},"accessibility":{"contrast":"AA"}}
        [[UI_DESIGN_ARTIFACT_JSON_END]]
        """;
        var plan = new GenerationPlan(
            "App",
            description,
            new TechStack(new[] { "TypeScript" }, new[] { "React" }, new[] { "PostgreSQL" }, new[] { "Docker" }, "r"),
            new[] { new GenerationPhase(1, "Scaffold", "desc", Array.Empty<AgentAssignment>()) },
            new[] { "CodeGenerationAgent" },
            "node:22-alpine",
            new[] { "npm ci" },
            new[] { "npm test" },
            4);

        await service.GenerateInitialAsync(plan, CancellationToken.None);

        ai.LastPrompt.Should().NotBeNull();
        ai.LastPrompt.Should().Contain("FRONTEND DESIGN ARTIFACT BINDING");
        ai.LastPrompt.Should().Contain("artifactId");
        ai.LastPrompt.Should().Contain("components");
        ai.LastPrompt.Should().Contain("screens");
    }

    private sealed class RecordingAiService : IAIService
    {
        private readonly string _response;
        public string? LastPrompt { get; private set; }
        public RecordingAiService(string response) => _response = response;
        public Task<string> GenerateCompletionAsync(string prompt, string? systemPrompt = null, string? model = null)
        {
            LastPrompt = prompt;
            return Task.FromResult(_response);
        }
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
        public ProviderCapability? GetProvider(string providerId) => GetProviders().First();
        public ModelRoutingDecision RouteStage(string stage, StageModelRequirement requirement)
            => new(stage, "openrouter", "openrouter/auto", "test");
        public StageModelRequirement? GetStageRequirements(string stage)
            => new(stage, false, false, true, 32000, 2048, 0.01);
    }
}

