using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Security;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Teams;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AIToolingSynthesisTeamTemplateTests
{
    [Fact]
    public void TeamTemplateResolver_ShouldMatchBestTemplateByKeywords()
    {
        var repository = new InMemoryTeamTemplateRepository();
        repository.Upsert(new TeamTemplateDefinition(
            Id: "web-core",
            Name: "Web Core",
            Description: "Template for common web tasks.",
            AgentRoles: new[] { "api-designer", "backend-developer" },
            SkillPackIds: new[] { "web-development-pack" },
            TriggerKeywords: new[] { "api", "backend", "http" },
            MinimumRole: UserRole.ExternalUser));
        repository.Upsert(new TeamTemplateDefinition(
            Id: "graphql-team",
            Name: "GraphQL Team",
            Description: "Template for graphql architecture tasks.",
            AgentRoles: new[] { "graphql-architect", "backend-developer" },
            SkillPackIds: new[] { "graphql-pack" },
            TriggerKeywords: new[] { "graphql", "schema", "resolver" },
            MinimumRole: UserRole.ExternalUser));

        var resolver = new TeamTemplateResolver(repository, new StaticUserRoleProvider(UserRole.ExternalUser));
        var resolution = resolver.Resolve("Design graphql api schema and resolvers");

        resolution.Matched.Should().BeTrue();
        resolution.Template.Should().NotBeNull();
        resolution.Template!.Id.Should().Contain("graphql");
    }

    [Fact]
    public void TeamTemplateResolver_ShouldRespectMinimumRole()
    {
        var repository = new InMemoryTeamTemplateRepository();
        repository.Upsert(new TeamTemplateDefinition(
            Id: "internal-ops",
            Name: "Internal Ops",
            Description: "Restricted template.",
            AgentRoles: new[] { "it-ops-orchestrator" },
            SkillPackIds: Array.Empty<string>(),
            TriggerKeywords: new[] { "incident", "operations" },
            MinimumRole: UserRole.InternalDeveloper));

        var externalResolver = new TeamTemplateResolver(repository, new StaticUserRoleProvider(UserRole.ExternalUser));
        var internalResolver = new TeamTemplateResolver(repository, new StaticUserRoleProvider(UserRole.InternalDeveloper));

        externalResolver.Resolve("incident operations playbook").Matched.Should().BeFalse();
        internalResolver.Resolve("incident operations playbook").Matched.Should().BeTrue();
    }
}
