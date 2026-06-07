using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BuildExecutionLogHeuristicsTests
{
    [Fact]
    public void ExtractErrors_ParsesMavenJavaCompileLine()
    {
        var logs = new List<ConsoleLogEntry>
        {
            new(DateTime.UtcNow, "stderr", "[ERROR] backend/src/main/java/com/app/Demo.java:[10,5] cannot find symbol")
        };
        var execution = new ExecutionResult(false, 1, TimeSpan.Zero, logs);
        var files = new List<GeneratedFile>
        {
            new("backend/src/main/java/com/app/Demo.java", "java", "class Demo {}")
        };

        var errors = BuildExecutionLogHeuristics.ExtractErrors(execution, files);

        errors.Should().HaveCount(1);
        errors[0].FilePath.Should().Be("backend/src/main/java/com/app/Demo.java");
        errors[0].LineNumber.Should().Be(10);
    }

    [Fact]
    public void JavaMavenCompileRemediation_RemovesBrokenTestFile()
    {
        var plan = StackPlanHeuristics.AlignJavaReactFullStackPlan(
            new GenerationPlan(
                "Bank",
                "banking",
                StackPlanHeuristics.CreateJavaReactFullStackTechStack(),
                Array.Empty<GenerationPhase>(),
                Array.Empty<string>(),
                "eclipse-temurin:21-jdk",
                Array.Empty<string>(),
                Array.Empty<string>(),
                5),
            "java backend react");

        var files = new List<GeneratedFile>
        {
            new("backend/pom.xml", "xml", "<project><modelVersion>4.0.0</modelVersion></project>"),
            new("backend/src/main/java/com/app/App.java", "java", "package com.app; class App {}"),
            new("backend/src/test/java/com/app/AppTest.java", "java", "package com.app; class AppTest { void x() { new Missing(); } }")
        };

        var log = "[ERROR] backend/src/test/java/com/app/AppTest.java:[1,45] cannot find symbol";
        var changed = JavaMavenCompileRemediation.Apply(files, plan, log);

        changed.Should().BeGreaterThan(0);
        files.Should().NotContain(f => f.RelativePath.Contains("/src/test/", StringComparison.OrdinalIgnoreCase));
    }
}
