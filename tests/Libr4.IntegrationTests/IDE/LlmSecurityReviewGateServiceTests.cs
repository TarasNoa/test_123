using FluentAssertions;
using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class LlmSecurityReviewGateServiceTests
{
    [Fact]
    public void SelectFilesForReview_PrioritizesSecurityAndConfigPaths()
    {
        var files = new[]
        {
            new GeneratedFile("frontend/src/App.tsx", "typescript", "export default function App() {}"),
            new GeneratedFile("backend/src/main/java/com/app/security/JwtFilter.java", "java", "class JwtFilter {}"),
            new GeneratedFile("backend/src/main/resources/application.properties", "properties", "jwt.secret=abc="),
        };

        var selected = LlmSecurityReviewGateService.SelectFilesForReview(files, 2);

        selected.Should().HaveCount(2);
        selected.Select(f => f.RelativePath).Should().Contain("backend/src/main/java/com/app/security/JwtFilter.java");
        selected.Select(f => f.RelativePath).Should().Contain("backend/src/main/resources/application.properties");
    }

    [Fact]
    public async Task EvaluateArtifactsAsync_ApprovesBankingJwtPatterns_WhenAgentPasses()
    {
        var ai = new Mock<IAIService>();
        ai.Setup(x => x.GenerateCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .ReturnsAsync("""
                {
                  "score": 9,
                  "passed": true,
                  "findings": []
                }
                """);

        var service = new LlmSecurityReviewGateService(
            ai.Object,
            CreateTestMatrix(),
            Options.Create(new SecurityReviewGateOptions { MinScore = 7, Mode = "llm" }),
            NullLogger<LlmSecurityReviewGateService>.Instance);

        var files = new[]
        {
            new GeneratedFile(
                "backend/src/main/java/com/apex/banking/security/JwtAuthFilter.java",
                "java",
                """
                UsernamePasswordAuthenticationToken authentication =
                    new UsernamePasswordAuthenticationToken(userDetails, null, userDetails.getAuthorities());
                if (bearerToken.startsWith("Bearer ")) { return bearerToken.substring(7); }
                """),
            new GeneratedFile(
                "backend/src/main/resources/application.properties",
                "properties",
                "jwt.secret=VGhlc2VjcmV0a2V5Zm9yYXBleGJhbmtpbmd0b2tlbnNpZ25pbmc=\n"),
        };

        var result = await service.EvaluateArtifactsAsync("post_generation", files, BuildTestPlan());

        result.Passed.Should().BeTrue();
        result.Score.Should().BeGreaterOrEqualTo(7);
        result.Reasons.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateArtifactsAsync_Fails_WhenAgentReportsCriticalFinding()
    {
        var ai = new Mock<IAIService>();
        ai.Setup(x => x.GenerateCompletionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .ReturnsAsync("""
                {
                  "score": 3,
                  "passed": false,
                  "findings": [
                    {
                      "severity": "critical",
                      "path": "backend/.env",
                      "category": "hardcoded_secret",
                      "message": "Production API key committed to repo",
                      "recommendation": "Load from environment"
                    }
                  ]
                }
                """);

        var service = new LlmSecurityReviewGateService(
            ai.Object,
            CreateTestMatrix(),
            Options.Create(new SecurityReviewGateOptions { MinScore = 7, Mode = "llm" }),
            NullLogger<LlmSecurityReviewGateService>.Instance);

        var result = await service.EvaluateArtifactsAsync(
            "post_generation",
            new[] { new GeneratedFile("backend/.env", "text", "STRIPE_KEY=sk_live_abc") },
            BuildTestPlan());

        result.Passed.Should().BeFalse();
        result.Reasons.Should().Contain(r => r.Contains("critical", StringComparison.OrdinalIgnoreCase));
    }

    private static DefaultProviderCapabilityMatrix CreateTestMatrix()
    {
        var options = Options.Create(new ProviderMatrixOptions
        {
            DefaultProvider = "dockermodelrunner",
            ReasoningModel = "test-reasoning",
            CodeGenerationModel = "test-coder"
        });
        return new DefaultProviderCapabilityMatrix(
            NullLogger<DefaultProviderCapabilityMatrix>.Instance,
            options);
    }

    private static GenerationPlan BuildTestPlan() =>
        new(
            applicationName: "TestApp",
            applicationDescription: "Test application",
            techStack: new TechStack(
                languages: new[] { "Java" },
                frameworks: new[] { "Spring Boot" },
                databases: Array.Empty<string>(),
                infrastructure: Array.Empty<string>(),
                rationale: "test"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "eclipse-temurin:21",
            buildCommands: Array.Empty<string>(),
            testCommands: Array.Empty<string>(),
            maxIterations: 1);
}
