using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Rules;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

/// <summary>
/// P1-1 wiring tests: ReviewGate2 layered with semantic rules. Demonstrates how Roslyn-backed
/// rules suppress false-positive PASS verdicts that legacy substring matching would emit when
/// only a README/comment mentions JWT/auth.
/// </summary>
public sealed class ReviewGate2WithSemanticRulesTests
{
    [Fact]
    public void LegacyMode_NoRules_ReadmeMentioningJwt_StillPassesLegacyChecks()
    {
        var sut = new ReviewGate2Service(NullLogger<ReviewGate2Service>.Instance);
        var (plan, files) = MakeAuthLikeReadme();

        var checklist = sut.EvaluateArchitectureChecklist(files, plan);

        // Legacy substring rule for error_handling/security should still pass — that's the bug.
        // No semantic rules are wired, so we just assert the call doesn't throw and produces items.
        checklist.Should().NotBeEmpty();
    }

    [Fact]
    public void WithAuthSemanticRule_ReadmeOnlyMentionsJwt_AuthCheckOverriddenToFail()
    {
        var sut = new ReviewGate2Service(
            NullLogger<ReviewGate2Service>.Instance,
            new IArchitectureCheckRule[] { new AuthImplementationRule_DotNet() });
        var (plan, files) = MakeAuthLikeReadme();

        var checklist = sut.EvaluateArchitectureChecklist(files, plan);

        var authItem = checklist.FirstOrDefault(i => i.ItemId == "auth_implementation");
        authItem.Should().NotBeNull("rule appended its own auth_implementation entry");
        authItem!.Satisfied.Should().BeFalse(
            "Roslyn rule must reject README-only mentions; legacy substring rule would have falsely passed");
        authItem.RemediationHint.Should().NotBeNull();
    }

    [Fact]
    public void WithAuthSemanticRule_RealAddAuthenticationCall_PassesAuthCheck()
    {
        var sut = new ReviewGate2Service(
            NullLogger<ReviewGate2Service>.Instance,
            new IArchitectureCheckRule[] { new AuthImplementationRule_DotNet() });
        var plan = MakeDotNetPlan();
        var files = new[]
        {
            new GeneratedFile("src/App/Program.cs", "csharp",
@"var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication().AddJwtBearer();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.Run();"),
            new GeneratedFile("README.md", "markdown", "# x")
        };

        var checklist = sut.EvaluateArchitectureChecklist(files, plan);
        var authItem = checklist.FirstOrDefault(i => i.ItemId == "auth_implementation");
        authItem.Should().NotBeNull();
        authItem!.Satisfied.Should().BeTrue();
    }

    [Fact]
    public void SemanticRule_ThrowingException_DoesNotCrashReviewGate()
    {
        var sut = new ReviewGate2Service(
            NullLogger<ReviewGate2Service>.Instance,
            new IArchitectureCheckRule[] { new ThrowingRule() });
        var (plan, files) = MakeAuthLikeReadme();

        Action act = () => sut.EvaluateArchitectureChecklist(files, plan);

        act.Should().NotThrow();
    }

    private static (GenerationPlan, IReadOnlyList<GeneratedFile>) MakeAuthLikeReadme()
    {
        var plan = MakeDotNetPlan();
        var files = new[]
        {
            new GeneratedFile("src/App/Program.cs", "csharp",
                @"// We document JWT auth in the README, but it's not wired in code.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet(""/"", () => ""hello"");
app.Run();"),
            new GeneratedFile("README.md", "markdown",
                "# App\n\nThis service uses JWT and OAuth and Authorize attributes.")
        };
        return (plan, files);
    }

    private static GenerationPlan MakeDotNetPlan() => new GenerationPlan(
        "App", "Build ASP.NET Core API",
        new TechStack(new[] { "C#" }, new[] { "ASP.NET Core" }, Array.Empty<string>(), Array.Empty<string>(), "test"),
        Array.Empty<GenerationPhase>(),
        Array.Empty<string>(),
        "mcr.microsoft.com/dotnet/sdk:8.0",
        new[] { "dotnet build" },
        new[] { "dotnet test" });

    private sealed class ThrowingRule : IArchitectureCheckRule
    {
        public string CheckId => "throwing_rule_test";
        public bool AppliesTo(GenerationPlan plan) => true;
        public Task<ArchitectureCheckOutcome> EvaluateAsync(
            IReadOnlyList<GeneratedFile> files,
            GenerationPlan plan,
            CancellationToken ct) =>
            throw new InvalidOperationException("simulated rule failure");
    }
}
