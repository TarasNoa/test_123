using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SecurityRemediationTests
{
    private static GenerationPlan JavaBankPlan() =>
        StackPlanHeuristics.AlignJavaReactFullStackPlan(
            new GenerationPlan(
                "BankCore",
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
    public void SecurityRemediationContextPolicy_OnlyIncludesFindingAndRelatedFiles()
    {
        var files = new List<GeneratedFile>
        {
            new("backend/src/main/java/com/bank/TransferService.java", "java", "class TransferService {}"),
            new("backend/src/main/java/com/bank/AccountService.java", "java", "class AccountService {}"),
            new("backend/application.yml", "yaml", "jwt.secret: hardcoded"),
            new("frontend/src/App.tsx", "typescript", "export default function App() { return null; }")
        };

        var errors = new[]
        {
            new ErrorReport(
                "SecurityFinding",
                "race on transfer idempotency",
                "serialize transfers",
                "backend/src/main/java/com/bank/TransferService.java",
                1)
        };

        var context = SecurityRemediationContextPolicy.BuildContext(files, errors);

        context.Should().HaveCountLessThanOrEqualTo(10);
        context.Select(f => f.RelativePath).Should().Contain("backend/src/main/java/com/bank/TransferService.java");
        context.Select(f => f.RelativePath).Should().NotContain("frontend/src/App.tsx");
        context.Select(f => f.RelativePath).Should().NotContain("backend/src/main/java/com/bank/AccountService.java");
    }

    [Fact]
    public void JavaSpringSecurityRemediation_ExternalizesJwtSecret()
    {
        var plan = JavaBankPlan();
        var files = new List<GeneratedFile>
        {
            new(
                "backend/src/main/resources/application.yml",
                "yaml",
                "jwt.secret: hardcoded-dev-secret\n")
        };

        JavaSpringSecurityRemediation.Apply(files, plan).Should().Be(1);
        files[0].Content.Should().Contain("${APP_JWT_SECRET:}");
        files[0].Content.Should().NotContain("hardcoded-dev-secret");
    }
}
