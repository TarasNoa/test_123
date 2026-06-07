using FluentAssertions;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;
using Libr4.IDE.Application.AutonomousAppGeneration.ModelRouting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AgentModelRouterTests
{
    [Fact]
    public void Route_ExploreRole_DmrProfile_UsesFastReasoningModel()
    {
        var router = CreateRouter(new AgentModelRoutingOptions
        {
            ActiveProfile = AgentModelProfile.Dmr,
            Roles = new Dictionary<string, AgentModelRoleOptions>(StringComparer.OrdinalIgnoreCase)
            {
                [AgentModelRoleNames.Explore] = new()
                {
                    DmrModel = "dmr-explore",
                    OpenRouterModel = "openai/gpt-4o-mini",
                    FallbackChain = ["dmr-fallback"]
                }
            }
        });

        var decision = router.Route(AgentModelRoleNames.Explore);
        decision.PrimaryModel.Should().Be("dmr-explore");
        decision.Profile.Should().Be(AgentModelProfile.Dmr);
        decision.FallbackModels.Should().Contain("dmr-fallback");
    }

    [Fact]
    public void Route_OpenRouterProfile_UsesCloudModel()
    {
        var router = CreateRouter(new AgentModelRoutingOptions
        {
            ActiveProfile = AgentModelProfile.OpenRouter,
            Roles = new Dictionary<string, AgentModelRoleOptions>(StringComparer.OrdinalIgnoreCase)
            {
                [AgentModelRoleNames.Implementer] = new()
                {
                    OpenRouterModel = "deepseek/deepseek-chat",
                    DmrModel = "local-coder"
                }
            }
        });

        router.Route(AgentModelRoleNames.Implementer).PrimaryModel.Should().Be("deepseek/deepseek-chat");
    }

    [Fact]
    public void Route_YamlOverride_TakesPrecedence()
    {
        var router = CreateRouter(new AgentModelRoutingOptions { ActiveProfile = AgentModelProfile.Dmr });
        router.Route(AgentModelRoleNames.Repair, "custom/spec-model").PrimaryModel.Should().Be("custom/spec-model");
    }

    [Fact]
    public void Route_BatchProfileActive_UsesBatchOverride()
    {
        var router = CreateRouter(new AgentModelRoutingOptions { ActiveProfile = AgentModelProfile.Dmr });
        using (LlmCallPreferenceContext.Activate(new LlmCallPreferences("batch/model", DisableStreaming: true)))
        {
            router.Route(AgentModelRoleNames.Verify).PrimaryModel.Should().Be("batch/model");
            router.Route(AgentModelRoleNames.Verify).Profile.Should().Be(AgentModelProfile.Batch);
        }
    }

    [Fact]
    public void RoleCircuit_OpensAfterRepeatedFailures()
    {
        var router = CreateRouter(new AgentModelRoutingOptions { RoleCircuitFailureThreshold = 2 });
        router.IsRoleModelCircuitOpen("repair", "m1").Should().BeFalse();
        router.RecordRoleModelFailure("repair", "m1");
        router.RecordRoleModelFailure("repair", "m1");
        router.IsRoleModelCircuitOpen("repair", "m1").Should().BeTrue();
        router.RecordRoleModelSuccess("repair", "m1");
        router.IsRoleModelCircuitOpen("repair", "m1").Should().BeFalse();
    }

    [Fact]
    public void FromPipelineStage_MapsFixingToRepair()
    {
        AgentModelRoleNames.FromPipelineStage("fixing").Should().Be(AgentModelRoleNames.Repair);
        AgentModelRoleNames.FromPipelineStage("generation").Should().Be(AgentModelRoleNames.Implementer);
    }

    private static AgentModelRouter CreateRouter(AgentModelRoutingOptions options)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:DefaultProvider"] = "DockerModelRunner"
            })
            .Build();

        return new AgentModelRouter(
            Options.Create(options),
            Options.Create(new ProviderMatrixOptions
            {
                FallbackModel = "fallback/default",
                LocalModel = "local/default",
                CodeGenerationModel = "local/coder",
                ReasoningModel = "local/reasoning",
                ApiModel = "openai/gpt-4o-mini"
            }),
            Options.Create(new AutonomousBatchLlmProfileOptions { Model = "batch/default" }),
            new RoleModelCircuitBreaker(Options.Create(options)),
            configuration,
            NullLogger<AgentModelRouter>.Instance);
    }
}
