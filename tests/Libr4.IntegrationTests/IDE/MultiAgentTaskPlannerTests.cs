using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.AutonomousAppGeneration.Agents;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class MultiAgentTaskPlannerTests
{
    private static GenerationPlan JavaBankPlan() =>
        StackPlanHeuristics.AlignJavaReactFullStackPlan(
            new GenerationPlan(
                "MobileBankApp",
                "banking",
                StackPlanHeuristics.CreateJavaReactFullStackTechStack(),
                Array.Empty<GenerationPhase>(),
                Array.Empty<string>(),
                "eclipse-temurin:21-jdk",
                Array.Empty<string>(),
                Array.Empty<string>(),
                6),
            "java react banking");

    [Fact]
    public void CreateTasksForPhase_BackendSubagents_DoNotReferenceParentSubtaskList()
    {
        var plan = JavaBankPlan();
        var tasks = MultiAgentTaskPlanner.CreateTasksForPhase(AgentPhase.Backend, plan, includeSubagentRoles: true);

        tasks.Should().NotBeEmpty();
        foreach (var parent in tasks.Where(t => t.Subtasks.Count > 0))
        {
            foreach (var sub in parent.Subtasks)
            {
                sub.Context.Task.Should().BeSameAs(sub);
                sub.Context.Task!.Subtasks.Should().BeEmpty();
                sub.Context.Task.Should().NotBeSameAs(parent);
            }
        }
    }
}
