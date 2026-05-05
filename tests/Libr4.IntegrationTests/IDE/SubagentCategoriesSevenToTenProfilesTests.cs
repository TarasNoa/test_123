using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Subagents;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SubagentCategoriesSevenToTenProfilesTests
{
    [Fact]
    public void Repository_ShouldExposeCategoriesSevenToTenProfiles()
    {
        var repository = new InMemorySubagentProfileRepository();
        var all = repository.List();

        all.Should().Contain(x => x.Id == "fintech-specialist");
        all.Should().Contain(x => x.Id == "healthcare-systems-engineer");
        all.Should().Contain(x => x.Id == "iot-engineer");
        all.Should().Contain(x => x.Id == "gameplay-engineer");
        all.Should().Contain(x => x.Id == "embedded-systems-engineer");
        all.Should().Contain(x => x.Id == "blockchain-engineer");
        all.Should().Contain(x => x.Id == "erp-specialist");
        all.Should().Contain(x => x.Id == "crm-specialist");
        all.Should().Contain(x => x.Id == "edtech-specialist");
        all.Should().Contain(x => x.Id == "legaltech-specialist");

        all.Should().Contain(x => x.Id == "product-manager-agent");
        all.Should().Contain(x => x.Id == "business-analyst-agent");
        all.Should().Contain(x => x.Id == "growth-engineer");
        all.Should().Contain(x => x.Id == "monetization-specialist");
        all.Should().Contain(x => x.Id == "customer-success-analyst");
        all.Should().Contain(x => x.Id == "marketing-automation-specialist");
        all.Should().Contain(x => x.Id == "sales-ops-engineer");
        all.Should().Contain(x => x.Id == "operations-optimizer");
        all.Should().Contain(x => x.Id == "strategy-analyst");
        all.Should().Contain(x => x.Id == "pricing-analyst");

        all.Should().Contain(x => x.Id == "multi-agent-coordinator");
        all.Should().Contain(x => x.Id == "task-distributor");
        all.Should().Contain(x => x.Id == "workflow-orchestrator");
        all.Should().Contain(x => x.Id == "agent-organizer");
        all.Should().Contain(x => x.Id == "context-manager");
        all.Should().Contain(x => x.Id == "error-coordinator");
        all.Should().Contain(x => x.Id == "knowledge-synthesizer");
        all.Should().Contain(x => x.Id == "performance-monitor");
        all.Should().Contain(x => x.Id == "agent-installer");
        all.Should().Contain(x => x.Id == "it-ops-orchestrator");
        all.Should().Contain(x => x.Id == "pied-piper");

        all.Should().Contain(x => x.Id == "research-analyst");
        all.Should().Contain(x => x.Id == "competitive-intelligence-agent");
        all.Should().Contain(x => x.Id == "trend-spotter");
        all.Should().Contain(x => x.Id == "experiment-designer");
        all.Should().Contain(x => x.Id == "quant-analyst");
        all.Should().Contain(x => x.Id == "qualitative-researcher");
        all.Should().Contain(x => x.Id == "market-research-specialist");
        all.Should().Contain(x => x.Id == "risk-analyst");
        all.Should().Contain(x => x.Id == "ops-research-specialist");
        all.Should().Contain(x => x.Id == "signal-detection-agent");
    }

    [Fact]
    public void Selector_ShouldResolveMetaOrchestrationAndResearchRoles()
    {
        var selector = new SubagentSelector(new InMemorySubagentProfileRepository());
        var selected = selector.SelectByRoles(new[]
        {
            "multi-agent-coordinator",
            "workflow-orchestrator",
            "research-analyst",
            "unknown-role"
        });

        selected.Select(x => x.Id).Should().Contain("multi-agent-coordinator");
        selected.Select(x => x.Id).Should().Contain("workflow-orchestrator");
        selected.Select(x => x.Id).Should().Contain("research-analyst");
        selected.Should().HaveCount(3);
    }

    [Fact]
    public void Repository_ShouldContainOneHundredPlusProfilesForCategoriesOneToTen()
    {
        var repository = new InMemorySubagentProfileRepository();
        repository.List().Should().HaveCountGreaterOrEqualTo(103);
    }
}
