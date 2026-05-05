using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Artifacts;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Mcp;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Security;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Skills;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AIToolingSynthesisPriority1Tests
{
    [Fact]
    public void ArtifactGenerator_ShouldCreateStructuredArtifact()
    {
        var generator = new ArtifactGenerator();

        var artifact = generator.Create(
            ArtifactType.Plan,
            "Billing plan",
            "{\"steps\":[\"design\",\"build\"]}",
            "agent");

        artifact.Id.Should().NotBeEmpty();
        artifact.Type.Should().Be(ArtifactType.Plan);
        artifact.Title.Should().Be("Billing plan");
        artifact.Source.Should().Be("agent");
    }

    [Fact]
    public void McpAdapterRegistry_ShouldRegisterAndQueryDatasources()
    {
        var registry = new McpAdapterRegistry();
        registry.Register(new McpDatasourceAdapter("figma", "Figma", "design", true));

        registry.IsRegistered("figma").Should().BeTrue();
        registry.GetAll().Should().ContainSingle(x => x.Id == "figma");
    }

    [Theory]
    [InlineData("small refactor", 200, 0, ExecutionMode.Copilot)]
    [InlineData("production architecture migration", 1200, 0, ExecutionMode.Flow)]
    [InlineData("quick fix", 200, 2, ExecutionMode.Agent)]
    public void FlowModeOrchestrator_ShouldSelectExpectedMode(string request, int contextLength, int errors, ExecutionMode expected)
    {
        var orchestrator = new FlowModeOrchestrator();
        orchestrator.SelectMode(request, contextLength, errors).Should().Be(expected);
    }

    [Fact]
    public void SkillPackGovernance_ShouldRespectMinimumRole()
    {
        var pack = new SkillPackDefinition(
            "finance-pack",
            "1.0.0",
            "finance",
            new[] { "risk-analysis", "ledger-audit" },
            UserRole.InternalDeveloper);

        var externalGovernance = new SkillPackGovernanceService(new StaticUserRoleProvider(UserRole.ExternalUser));
        var internalGovernance = new SkillPackGovernanceService(new StaticUserRoleProvider(UserRole.InternalDeveloper));

        externalGovernance.CanUse(pack).Should().BeFalse();
        internalGovernance.CanUse(pack).Should().BeTrue();
    }
}
