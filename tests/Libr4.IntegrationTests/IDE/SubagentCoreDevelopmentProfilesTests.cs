using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Subagents;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SubagentCoreDevelopmentProfilesTests
{
    [Fact]
    public void Repository_ShouldExposeAllCoreDevelopmentProfiles()
    {
        var repository = new InMemorySubagentProfileRepository();
        var all = repository.List();

        all.Should().Contain(x => x.Id == "api-designer");
        all.Should().Contain(x => x.Id == "backend-developer");
        all.Should().Contain(x => x.Id == "frontend-developer");
        all.Should().Contain(x => x.Id == "fullstack-developer");
        all.Should().Contain(x => x.Id == "code-mapper");
        all.Should().Contain(x => x.Id == "graphql-architect");
        all.Should().Contain(x => x.Id == "microservices-architect");
        all.Should().Contain(x => x.Id == "ui-designer");
        all.Should().Contain(x => x.Id == "ui-fixer");
        all.Should().Contain(x => x.Id == "websocket-engineer");
        all.Should().Contain(x => x.Id == "electron-pro");
        all.Should().Contain(x => x.Id == "mobile-developer");
    }

    [Fact]
    public void Selector_ShouldResolveProfilesForRequestedRoles()
    {
        var selector = new SubagentSelector(new InMemorySubagentProfileRepository());
        var selected = selector.SelectByRoles(new[] { "graphql-architect", "backend-developer", "unknown-role" });

        selected.Should().HaveCount(2);
        selected.Select(x => x.Id).Should().Contain("graphql-architect");
        selected.Select(x => x.Id).Should().Contain("backend-developer");
    }
}
