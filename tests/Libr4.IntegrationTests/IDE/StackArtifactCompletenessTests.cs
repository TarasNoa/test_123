using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class StackArtifactCompletenessTests
{
    [Fact]
    public void MeetsPlanMinimum_JavaReact_RequiresMonorepoSpine()
    {
        var dotnetPlan = new GenerationPlan(
            "MobileBank",
            "banking",
            new TechStack(
                new[] { "C#" },
                new[] { "ASP.NET Core" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "planner"),
            Array.Empty<GenerationPhase>(),
            new[] { "CodeGenerationAgent" },
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            4);

        var plan = StackPlanHeuristics.AlignJavaReactFullStackPlan(
            dotnetPlan,
            MobileBankingJavaReactStackTests.MobileBankingJavaReactPromptRu);

        var sparse = new List<GeneratedFile>
        {
            new("src/tests/apiClient.test.ts", "typescript", "test();")
        };
        StackArtifactCompleteness.MeetsPlanMinimum(plan, sparse).Should().BeFalse();
        StackArtifactCompleteness.SanitizeRelativePath("backend/pom.xml\njunk")
            .Should().Be("backend/pom.xml");
    }
}
