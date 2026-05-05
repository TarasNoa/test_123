using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Subagents;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SubagentLanguageAndInfrastructureProfilesTests
{
    [Fact]
    public void Repository_ShouldExposeLanguageSpecialistsAndInfrastructureProfiles()
    {
        var repository = new InMemorySubagentProfileRepository();
        var all = repository.List();

        all.Should().Contain(x => x.Id == "python-specialist");
        all.Should().Contain(x => x.Id == "csharp-specialist");
        all.Should().Contain(x => x.Id == "typescript-specialist");
        all.Should().Contain(x => x.Id == "go-specialist");
        all.Should().Contain(x => x.Id == "rust-specialist");
        all.Should().Contain(x => x.Id == "java-specialist");
        all.Should().Contain(x => x.Id == "kotlin-specialist");
        all.Should().Contain(x => x.Id == "swift-specialist");
        all.Should().Contain(x => x.Id == "php-specialist");
        all.Should().Contain(x => x.Id == "ruby-specialist");

        all.Should().Contain(x => x.Id == "devops-engineer");
        all.Should().Contain(x => x.Id == "platform-engineer");
        all.Should().Contain(x => x.Id == "cloud-architect");
        all.Should().Contain(x => x.Id == "kubernetes-operator");
        all.Should().Contain(x => x.Id == "terraform-specialist");
        all.Should().Contain(x => x.Id == "sre-engineer");
        all.Should().Contain(x => x.Id == "observability-agent");
        all.Should().Contain(x => x.Id == "security-platform-engineer");
        all.Should().Contain(x => x.Id == "database-operator");
        all.Should().Contain(x => x.Id == "network-engineer");
    }

    [Fact]
    public void Selector_ShouldResolveInfrastructureRoleUsedByTeamTemplate()
    {
        var selector = new SubagentSelector(new InMemorySubagentProfileRepository());
        var selected = selector.SelectByRoles(new[] { "backend-developer", "microservices-architect", "observability-agent" });

        selected.Select(x => x.Id).Should().Contain("observability-agent");
    }

    [Fact]
    public void Repository_ShouldContainThirtyPlusProfilesForCategoriesOneToThree()
    {
        var repository = new InMemorySubagentProfileRepository();
        repository.List().Should().HaveCountGreaterOrEqualTo(32);
    }
}
