using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SecurityReviewGateServiceTests
{
    [Fact]
    public async Task EvaluateArtifacts_ShouldDetectInsecureDefaultSecrets()
    {
        var service = new SecurityReviewGateService(Options.Create(new SecurityReviewGateOptions()));

        var files = new[]
        {
            new GeneratedFile("config/settings.py", "python", "SECRET_KEY='dev-secret-change-me'")
        };

        var result = await service.EvaluateArtifactsAsync("generation", files, BuildTestPlan());

        result.Score.Should().BeLessThan(10);
        result.Reasons.Should().Contain(r => r.Contains("insecure_default_secret"));
        result.Passed.Should().BeFalse();
    }

    [Fact]
    public void EvaluateArtifacts_ShouldAllowTestTokenWithAuthFlow()
    {
        var service = new SecurityReviewGateService(Options.Create(new SecurityReviewGateOptions()));

        var files = new[]
        {
            new GeneratedFile("config.py", "python", "TEST_TOKEN='abc123def456'\n# Auth flow implemented with JWT validation")
        };

        var result = service.EvaluateArtifactsAsync("generation", files, BuildTestPlan()).GetAwaiter().GetResult();

        result.Score.Should().Be(10);
        result.Reasons.Should().NotContain(r => r.Contains("test_token_without_auth"));
    }

    [Fact]
    public async Task EvaluateArtifacts_ShouldSkipTestFiles()
    {
        var service = new SecurityReviewGateService(Options.Create(new SecurityReviewGateOptions()));

        var files = new[]
        {
            new GeneratedFile("tests/test_config.py", "python", "SOME_CONFIG='value'")
        };

        var result = await service.EvaluateArtifactsAsync("generation", files, BuildTestPlan());

        result.Score.Should().Be(10);
        result.Reasons.Should().BeEmpty();
    }

    private static GenerationPlan BuildTestPlan()
    {
        return new GenerationPlan(
            applicationName: "TestApp",
            applicationDescription: "Test application",
            techStack: new TechStack(
                languages: new[] { "Python" },
                frameworks: new[] { "FastAPI" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "python:3.11-slim",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);
    }
}
