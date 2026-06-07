using System.Text.RegularExpressions;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ProjectValidationPipelineTests
{
    [Fact]
    public void RunPostGeneration_MergesDuplicatePomBuildTags()
    {
        var plan = StackPlanHeuristics.AlignJavaReactFullStackPlan(
            new GenerationPlan("MobileBank", "banking", StackPlanHeuristics.CreateJavaReactFullStackTechStack(),
                Array.Empty<GenerationPhase>(), Array.Empty<string>(), "eclipse-temurin:21-jdk",
                Array.Empty<string>(), Array.Empty<string>(), 5),
            "java react");

        var pom = """
            <project>
              <modelVersion>4.0.0</modelVersion>
              <build><plugins></plugins></build>
              <build><plugins><plugin><artifactId>maven-surefire-plugin</artifactId></plugin></plugins></build>
            </project>
            """;

        var files = new List<GeneratedFile>
        {
            new("backend/pom.xml", "xml", pom),
            new("backend/src/main/java/com/mobilebank/MobileBankApplication.java", "java",
                "@SpringBootApplication\npublic class MobileBankApplication {}"),
            new("backend/src/main/java/com/mobilebank/MobilebankApiApplication.java", "java",
                "@SpringBootApplication\npublic class MobilebankApiApplication {}"),
        };

        var result = ProjectValidationPipeline.RunPostGeneration(files, plan);

        result.Warnings.Should().Contain(w =>
            w.Contains("POM_DUPLICATE_BUILD_TAG", StringComparison.OrdinalIgnoreCase)
            || w.Contains("JAVA_MULTIPLE_SPRING_BOOT_MAIN", StringComparison.OrdinalIgnoreCase));
        Regex.Matches(files.Single(f => f.RelativePath == "backend/pom.xml").Content!, "<build>", RegexOptions.IgnoreCase)
            .Count.Should().Be(1);
        files.Should().NotContain(f => f.RelativePath.Contains("MobilebankApiApplication", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RepairErrorClassifier_ClassifiesDuplicateBuildAsPomSyntax()
    {
        var errors = new List<ErrorReport>
        {
            new("build", "Non-parseable POM: Duplicated tag: 'build'", "merge duplicate build sections", "backend/pom.xml")
        };

        var classified = RepairErrorClassifier.Classify(errors, "Duplicated tag: build");
        classified[0].Class.Should().Be(RepairErrorClassifier.RepairErrorClass.PomSyntax);
        RepairErrorClassifier.ShouldSkipLlmFixer(classified).Should().BeTrue();
    }

    [Fact]
    public void RepairErrorClassifier_ClassifiesCompileError_AsLevel2_NotRuntimeSkip()
    {
        var errors = new List<ErrorReport>
        {
            new("CompileError", "AccountService references findByUserId which does not exist", "add repository method", "backend/src/main/java/com/generated/banking/service/AccountService.java")
        };

        var classified = RepairErrorClassifier.Classify(errors, "compileerror findByUserId");
        classified[0].Class.Should().Be(RepairErrorClassifier.RepairErrorClass.CompileSymbol);
        classified[0].Tier.Should().Be(RepairErrorClassifier.RepairTier.Level2Compile);
        RepairErrorClassifier.ShouldSkipLlmForRuntime(classified).Should().BeFalse();
    }

    [Fact]
    public void ResumeSeedLoader_LoadsExportedBankingSeed()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            ".logs", "resume-seeds", "20729f31-a895-480d-a429-653aba47080f.json"));
        if (!File.Exists(path))
            return;

        var snapshot = ResumeSeedLoader.TryLoad(path);
        snapshot.Should().NotBeNull();
        snapshot!.Files.Count.Should().BeGreaterThan(50);
        snapshot.Plan.ApplicationName.Should().Be("MobileBankingApp");
    }
}
