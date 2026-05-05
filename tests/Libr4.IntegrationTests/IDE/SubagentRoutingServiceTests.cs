using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Security;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Skills;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Teams;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SubagentRoutingServiceTests
{
    [Fact]
    public void Resolve_ShouldReturnRoleAndGovernedSkillPacks_WhenTemplateMatches()
    {
        var teamRepository = new InMemoryTeamTemplateRepository();
        teamRepository.Upsert(new TeamTemplateDefinition(
            Id: "graphql-team",
            Name: "GraphQL Team",
            Description: "GraphQL oriented team.",
            AgentRoles: new[] { "graphql-architect", "backend-developer" },
            SkillPackIds: new[] { "web-pack", "internal-pack" },
            TriggerKeywords: new[] { "graphql", "resolver" },
            MinimumRole: UserRole.ExternalUser));

        var teamResolver = new TeamTemplateResolver(teamRepository, new StaticUserRoleProvider(UserRole.ExternalUser));
        var skillRepository = new InMemorySkillPackRepository();
        skillRepository.Upsert(new SkillPackDefinition("web-pack", "1.0.0", "web", new[] { "graphql-schema" }, UserRole.ExternalUser));
        skillRepository.Upsert(new SkillPackDefinition("web-development-pack", "1.0.0", "web", new[] { "graphql-schema" }, UserRole.ExternalUser));
        skillRepository.Upsert(new SkillPackDefinition("internal-pack", "1.0.0", "ops", new[] { "incident-debug" }, UserRole.InternalDeveloper));

        var routing = new SubagentRoutingService(
            teamResolver,
            skillRepository,
            new SkillPackGovernanceService(new StaticUserRoleProvider(UserRole.ExternalUser)))
            .Resolve("Design graphql resolver architecture");

        routing.Matched.Should().BeTrue();
        routing.AgentRoles.Should().Contain("graphql-architect");
        Assert.Contains(routing.AllowedSkillPackIds, id => id == "web-pack" || id == "web-development-pack");
        routing.AllowedSkillPackIds.Should().NotContain("internal-pack");
    }

    [Fact]
    public void Resolve_ShouldReturnNoMatch_WhenNoTemplateTriggered()
    {
        var routing = new SubagentRoutingService(
            new TeamTemplateResolver(new InMemoryTeamTemplateRepository(), new StaticUserRoleProvider(UserRole.ExternalUser)),
            new InMemorySkillPackRepository(),
            new SkillPackGovernanceService(new StaticUserRoleProvider(UserRole.ExternalUser)))
            .Resolve("Simple text formatting helper");

        routing.Matched.Should().BeFalse();
        routing.AgentRoles.Should().BeEmpty();
        routing.AllowedSkillPackIds.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_ShouldReturnBuiltInGovernedSkillPacks_ForSecureAiTemplate()
    {
        var routing = new SubagentRoutingService(
            new TeamTemplateResolver(new InMemoryTeamTemplateRepository(), new StaticUserRoleProvider(UserRole.ExternalUser)),
            new InMemorySkillPackRepository(),
            new SkillPackGovernanceService(new StaticUserRoleProvider(UserRole.ExternalUser)))
            .Resolve("Build secure AI platform with LLM and RAG guardrails.");

        routing.Matched.Should().BeTrue();
        routing.TeamTemplateId.Should().Be("secure-ai-platform-team");
        routing.AllowedSkillPackIds.Should().Contain("devops-pack");
        routing.AllowedSkillPackIds.Should().Contain("web-development-pack");
        routing.AllowedSkillPackIds.Should().Contain("security-pack");
    }

    [Fact]
    public void Resolve_ShouldFilterInternalSkillPacks_WhenExternalUser()
    {
        var teamRepository = new InMemoryTeamTemplateRepository();
        teamRepository.Upsert(new TeamTemplateDefinition(
            Id: "test-team",
            Name: "Test Team",
            Description: "Test team.",
            AgentRoles: new[] { "test-agent" },
            SkillPackIds: new[] { "web-development-pack", "internal-ops-pack" },
            TriggerKeywords: new[] { "test" },
            MinimumRole: UserRole.ExternalUser));

        var teamResolver = new TeamTemplateResolver(teamRepository, new StaticUserRoleProvider(UserRole.ExternalUser));
        var skillRepository = new InMemorySkillPackRepository();
        var routing = new SubagentRoutingService(
            teamResolver,
            skillRepository,
            new SkillPackGovernanceService(new StaticUserRoleProvider(UserRole.ExternalUser)))
            .Resolve("Test request");

        routing.Matched.Should().BeTrue();
        routing.AllowedSkillPackIds.Should().Contain("web-development-pack");
        routing.AllowedSkillPackIds.Should().NotContain("internal-ops-pack");
    }

    [Fact]
    public void Resolve_ShouldAllowInternalSkillPacks_WhenInternalUser()
    {
        var teamRepository = new InMemoryTeamTemplateRepository();
        teamRepository.Upsert(new TeamTemplateDefinition(
            Id: "test-team",
            Name: "Test Team",
            Description: "Test team.",
            AgentRoles: new[] { "test-agent" },
            SkillPackIds: new[] { "web-development-pack", "internal-ops-pack" },
            TriggerKeywords: new[] { "test" },
            MinimumRole: UserRole.ExternalUser));

        var teamResolver = new TeamTemplateResolver(teamRepository, new StaticUserRoleProvider(UserRole.InternalDeveloper));
        var skillRepository = new InMemorySkillPackRepository();
        var routing = new SubagentRoutingService(
            teamResolver,
            skillRepository,
            new SkillPackGovernanceService(new StaticUserRoleProvider(UserRole.InternalDeveloper)))
            .Resolve("Test request");

        routing.Matched.Should().BeTrue();
        routing.AllowedSkillPackIds.Should().Contain("web-development-pack");
        routing.AllowedSkillPackIds.Should().Contain("internal-ops-pack");
    }

    [Fact]
    public void Resolve_ShouldRejectInternalTemplate_WhenExternalUser()
    {
        // Built-in internal-security-audit-team has MinimumRole = InternalDeveloper
        // Use "penetration test" trigger which is unique to internal template
        var teamResolver = new TeamTemplateResolver(new InMemoryTeamTemplateRepository(), new StaticUserRoleProvider(UserRole.ExternalUser));
        var routing = new SubagentRoutingService(
            teamResolver,
            new InMemorySkillPackRepository(),
            new SkillPackGovernanceService(new StaticUserRoleProvider(UserRole.ExternalUser)))
            .Resolve("Perform penetration test");

        routing.Matched.Should().BeFalse();
        routing.Reason.Should().Be("no_matching_template");
    }

    [Fact]
    public void Resolve_ShouldMatchInternalTemplate_WhenInternalUser()
    {
        var teamResolver = new TeamTemplateResolver(new InMemoryTeamTemplateRepository(), new StaticUserRoleProvider(UserRole.InternalDeveloper));
        var routing = new SubagentRoutingService(
            teamResolver,
            new InMemorySkillPackRepository(),
            new SkillPackGovernanceService(new StaticUserRoleProvider(UserRole.InternalDeveloper)))
            .Resolve("Perform security audit");

        routing.Matched.Should().BeTrue();
        routing.TeamTemplateId.Should().Be("internal-security-audit-team");
        routing.AllowedSkillPackIds.Should().Contain("security-pack");
        routing.AllowedSkillPackIds.Should().Contain("internal-ops-pack");
    }

    [Fact]
    public void Resolve_ShouldMatchInternalDataGovernanceTemplate_WhenInternalUser()
    {
        var teamResolver = new TeamTemplateResolver(new InMemoryTeamTemplateRepository(), new StaticUserRoleProvider(UserRole.InternalDeveloper));
        var routing = new SubagentRoutingService(
            teamResolver,
            new InMemorySkillPackRepository(),
            new SkillPackGovernanceService(new StaticUserRoleProvider(UserRole.InternalDeveloper)))
            .Resolve("Conduct data governance audit");

        routing.Matched.Should().BeTrue();
        routing.TeamTemplateId.Should().Be("internal-data-governance-team");
        routing.AllowedSkillPackIds.Should().Contain("security-pack");
        routing.AllowedSkillPackIds.Should().Contain("internal-ops-pack");
    }
}
