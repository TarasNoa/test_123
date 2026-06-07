using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ProviderCapabilityMatrixRoutingTests
{
    private static DefaultProviderCapabilityMatrix CreateMatrix(
        string? reasoning = "reasoning-35b",
        string? codegen = "coder-9b")
    {
        var options = Options.Create(new ProviderMatrixOptions
        {
            DefaultProvider = "dockermodelrunner",
            ReasoningModel = reasoning,
            CodeGenerationModel = codegen,
            LocalModel = reasoning
        });
        return new DefaultProviderCapabilityMatrix(NullLogger<DefaultProviderCapabilityMatrix>.Instance, options);
    }

    [Theory]
    [InlineData("planning", "reasoning-35b")]
    [InlineData("review", "reasoning-35b")]
    [InlineData("generation", "coder-9b")]
    [InlineData("fixing", "coder-9b")]
    [InlineData("consistency", "coder-9b")]
    public void RouteStage_DockerModelRunner_SelectsStageAppropriateModel(string stage, string expectedModel)
    {
        var matrix = CreateMatrix();
        var req = matrix.GetStageRequirements(stage)
                  ?? new StageModelRequirement(stage, false, false, false, 8000, 2048, 0.01);

        var decision = matrix.RouteStage(stage, req);

        decision.ProviderId.Should().Be("dockermodelrunner");
        decision.ModelId.Should().Be(expectedModel);
        decision.RoutingReason.Should().Contain("stage_model_routing");
    }

    [Fact]
    public void RouteStage_StageOverride_UsesConfiguredProviderAndReason()
    {
        var options = Options.Create(new ProviderMatrixOptions
        {
            DefaultProvider = "dockermodelrunner",
            ReasoningModel = "reasoning-35b",
            CodeGenerationModel = "coder-9b",
            StageOverrides = new Dictionary<string, StageProviderOverride>(StringComparer.OrdinalIgnoreCase)
            {
                ["planning"] = new StageProviderOverride { ProviderId = "openrouter", ModelId = "anthropic/claude-3.5-sonnet" }
            }
        });
        var matrix = new DefaultProviderCapabilityMatrix(
            NullLogger<DefaultProviderCapabilityMatrix>.Instance,
            options);
        var req = matrix.GetStageRequirements("planning")
                  ?? new StageModelRequirement("planning", false, false, false, 8000, 2048, 0.01);

        var decision = matrix.RouteStage("planning", req);

        decision.ProviderId.Should().Be("openrouter");
        decision.ModelId.Should().Be("anthropic/claude-3.5-sonnet");
        decision.RoutingReason.Should().Be("stage_override:planning");
    }
}
