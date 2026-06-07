using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ManifestRepairEngineTests
{
    [Fact]
    public void RepairMavenPoms_NormalizesLowercaseGroupIdTag()
    {
        var files = new List<GeneratedFile>
        {
            new("backend/pom.xml", "xml", """
                <project>
                  <modelversion>4.0.0</modelversion>
                  <groupid>com.example</groupid>
                  <artifactid>demo</artifactid>
                </project>
                """)
        };

        ManifestRepairEngine.RepairMavenPoms(files).Should().Be(1);
        files[0].Content.Should().Contain("<groupId>");
        files[0].Content.Should().NotContain("<groupid>", because: "invalid Maven tag casing");
        files[0].Content.Should().Contain("<modelVersion>");
        files[0].Content.Should().Contain("<artifactId>");
    }

    [Fact]
    public void RepairDuplicatePomBuildSections_MergesAdjacentBuildBlocks()
    {
        var files = new List<GeneratedFile>
        {
            new("backend/pom.xml", "xml", """
                <project>
                <build><plugins></plugins></build>
                <build><plugins><plugin><artifactId>x</artifactId></plugin></plugins></build>
                </project>
                """)
        };

        ManifestRepairEngine.RepairDuplicatePomBuildSections(files).Should().Be(1);
        files[0].Content!.Should().NotContain("</build>\n<build>");
    }

    [Fact]
    public void ApplyLevel0Recovery_ReturnsPatchWhenPomSyntaxBroken()
    {
        var plan = StackPlanHeuristics.AlignJavaReactFullStackPlan(
            new GenerationPlan("Bank", "x", StackPlanHeuristics.CreateJavaReactFullStackTechStack(),
                Array.Empty<GenerationPhase>(), Array.Empty<string>(), "eclipse-temurin:21-jdk",
                Array.Empty<string>(), Array.Empty<string>(), 5),
            "java react");

        var files = new List<GeneratedFile>
        {
            new("backend/pom.xml", "xml", "<project><groupid>com.bank</groupid></project>")
        };

        var errors = new[]
        {
            new ErrorReport("BuildError", "unrecognized tag 'groupid'", "backend/pom.xml")
        };

        var patches = RepairErrorClassifier.ApplyLevel0Recovery(files, plan, errors, "unrecognized tag 'groupid'");
        patches.Should().NotBeEmpty();
        patches[0].Content.Should().Contain("<groupId>");
    }
}
