using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class CompileRepairPlannerTests
{
    [Fact]
    public void BuildPlan_Prioritizes_PomRootCause()
    {
        var files = new List<GeneratedFile>
        {
            new("backend/pom.xml", "xml", "<project></project>"),
            new("backend/src/main/java/com/app/App.java", "java", "class App {}")
        };
        var logs = new List<ConsoleLogEntry>
        {
            new(DateTime.UtcNow, "stderr", "[ERROR] Non-parseable POM backend/pom.xml: Duplicated tag: 'build'")
        };
        var execution = new ExecutionResult(false, 1, TimeSpan.Zero, logs);
        var errors = new List<ErrorReport>
        {
            new("CompileError", "cannot find symbol User", "add User", "backend/src/main/java/com/app/App.java")
        };

        var plan = CompileRepairPlanner.BuildPlan(execution, files, errors);

        plan.RootCauseCategory.Should().Be("manifest_pom");
        plan.FixerErrors[0].FilePath.Should().Be("backend/pom.xml");
    }
}
