using System.Text.RegularExpressions;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class JavaStructuralCompileRemediationTests
{
    [Fact]
    public void FixDuplicatePomBuildSections_MergesIntoOne()
    {
        var plan = StackPlanHeuristics.AlignJavaReactFullStackPlan(
            new GenerationPlan("Bank", "x", StackPlanHeuristics.CreateJavaReactFullStackTechStack(),
                Array.Empty<GenerationPhase>(), Array.Empty<string>(), "eclipse-temurin:21-jdk",
                Array.Empty<string>(), Array.Empty<string>(), 5),
            "java react");

        var files = new List<GeneratedFile>
        {
            new("backend/pom.xml", "xml", """
                <project>
                <build><plugins><plugin><artifactId>spring-boot-maven-plugin</artifactId></plugin></plugins></build>
                <build><plugins><plugin><artifactId>maven-surefire-plugin</artifactId></plugin></plugins></build>
                </project>
                """)
        };

        JavaStructuralCompileRemediation.ApplyStructuralFixes(files, plan, null).Should().BeGreaterThan(0);
        files[0].Content!.Should().NotContain("</build>\n<build>", because: "duplicate build sections merged");
        Regex.Matches(files[0].Content!, "<build>", RegexOptions.IgnoreCase).Count.Should().Be(1);
    }
}
