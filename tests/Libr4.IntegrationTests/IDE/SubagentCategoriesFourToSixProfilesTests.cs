using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Subagents;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SubagentCategoriesFourToSixProfilesTests
{
    [Fact]
    public void Repository_ShouldExposeQualitySecurityDataAiAndDeveloperExperienceProfiles()
    {
        var repository = new InMemorySubagentProfileRepository();
        var all = repository.List();

        all.Should().Contain(x => x.Id == "test-automation-engineer");
        all.Should().Contain(x => x.Id == "security-auditor");
        all.Should().Contain(x => x.Id == "performance-engineer");
        all.Should().Contain(x => x.Id == "compliance-specialist");
        all.Should().Contain(x => x.Id == "chaos-engineer");
        all.Should().Contain(x => x.Id == "incident-responder");
        all.Should().Contain(x => x.Id == "vulnerability-researcher");
        all.Should().Contain(x => x.Id == "privacy-engineer");
        all.Should().Contain(x => x.Id == "supply-chain-security");
        all.Should().Contain(x => x.Id == "release-quality-gatekeeper");

        all.Should().Contain(x => x.Id == "ml-engineer");
        all.Should().Contain(x => x.Id == "data-engineer");
        all.Should().Contain(x => x.Id == "analytics-engineer");
        all.Should().Contain(x => x.Id == "llm-engineer");
        all.Should().Contain(x => x.Id == "rag-architect");
        all.Should().Contain(x => x.Id == "data-scientist");
        all.Should().Contain(x => x.Id == "feature-store-operator");
        all.Should().Contain(x => x.Id == "model-ops-engineer");
        all.Should().Contain(x => x.Id == "data-governance-agent");
        all.Should().Contain(x => x.Id == "ai-safety-reviewer");

        all.Should().Contain(x => x.Id == "developer-experience-engineer");
        all.Should().Contain(x => x.Id == "build-systems-engineer");
        all.Should().Contain(x => x.Id == "documentation-engineer");
        all.Should().Contain(x => x.Id == "onboarding-specialist");
        all.Should().Contain(x => x.Id == "cli-tooling-engineer");
        all.Should().Contain(x => x.Id == "ide-integration-engineer");
        all.Should().Contain(x => x.Id == "template-maintainer");
        all.Should().Contain(x => x.Id == "api-sdk-generator");
        all.Should().Contain(x => x.Id == "release-notes-writer");
        all.Should().Contain(x => x.Id == "workflow-automation-engineer");
    }

    [Fact]
    public void Selector_ShouldResolveCategoryFourToSixRoles()
    {
        var selector = new SubagentSelector(new InMemorySubagentProfileRepository());
        var selected = selector.SelectByRoles(new[]
        {
            "security-auditor",
            "llm-engineer",
            "developer-experience-engineer",
            "unknown-role"
        });

        selected.Select(x => x.Id).Should().Contain("security-auditor");
        selected.Select(x => x.Id).Should().Contain("llm-engineer");
        selected.Select(x => x.Id).Should().Contain("developer-experience-engineer");
        selected.Should().HaveCount(3);
    }

    [Fact]
    public void Repository_ShouldContainSixtyPlusProfilesForCategoriesOneToSix()
    {
        var repository = new InMemorySubagentProfileRepository();
        repository.List().Should().HaveCountGreaterOrEqualTo(62);
    }
}
