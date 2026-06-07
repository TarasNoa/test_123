using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting.Templates;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PromptTemplateTests
{
    private static PromptTemplateRegistry CreateRegistry() =>
        new(
            Options.Create(new PromptTemplateOptions { EnableInstructionTemplates = true }),
            new PromptVariantSelector(Options.Create(new PromptTemplateOptions())));

    [Fact]
    public void InstructionTemplateFormatter_WrapsInstructionAndResponse()
    {
        var formatted = InstructionTemplateFormatter.Format("Fix the build", "JSON only");
        formatted.Should().Contain("### Instruction:");
        formatted.Should().Contain("### Response:");
        formatted.Should().Contain("Fix the build");
        formatted.Should().Contain("JSON only");
    }

    [Fact]
    public void Registry_ContainsAllReservedRoles()
    {
        var registry = CreateRegistry();
        foreach (var role in new[] { "implementer", "explore", "verify", "repair", "computer" })
            registry.TryGet(role, "v1").Should().NotBeNull($"role {role}");
    }

    [Fact]
    public void VariantSelector_IsDeterministicPerRunId()
    {
        var selector = new PromptVariantSelector(Options.Create(new PromptTemplateOptions
        {
            AbVariants = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["repair"] = ["v1", "v2"]
            }
        }));
        var runId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        selector.SelectVariant("repair", runId).Should().Be(selector.SelectVariant("repair", runId));
    }

    [Fact]
    public void BuildSystemPrompt_AppliesInstructionTemplate()
    {
        var registry = CreateRegistry();
        var resolver = new BuiltinPromptVarResolver();
        var context = new BuiltinPromptVarContext();

        var prompt = AgentPromptBuilder.BuildSystemPrompt(
            isGeneration: false,
            BuiltinPromptStage.Repairing,
            resolver,
            context,
            registry,
            "v1");

        prompt.Should().Contain("### Instruction:");
        prompt.Should().Contain("### Response:");
        prompt.Should().Contain("repair subagent");
    }

    [Fact]
    public void BuildUserObjective_WrapsWhenTemplateEnabled()
    {
        var registry = new Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core.AgentToolRegistry(
            Array.Empty<Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions.IAgentTool>());
        var plan = new GenerationPlan(
            applicationName: "App",
            applicationDescription: "Desc",
            techStack: new TechStack(["Python"], ["Django"], [], [], "django"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.12-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>());

        var objective = AgentPromptBuilder.BuildUserObjective(
            "Fix imports",
            plan,
            buildLog: "error",
            registry,
            useInstructionTemplate: true);

        objective.Should().StartWith("### Instruction:");
        objective.Should().Contain("### Response:");
    }
}
