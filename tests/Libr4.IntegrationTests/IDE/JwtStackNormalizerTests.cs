using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class JwtStackNormalizerTests
{
    [Fact]
    public void Normalize_RemovesDuplicateJwtProviders()
    {
        var plan = StackPlanHeuristics.AlignJavaReactFullStackPlan(
            new GenerationPlan("Bank", "x", StackPlanHeuristics.CreateJavaReactFullStackTechStack(),
                Array.Empty<GenerationPhase>(), Array.Empty<string>(), "eclipse-temurin:21-jdk",
                Array.Empty<string>(), Array.Empty<string>(), 5),
            "java react");

        var files = new List<GeneratedFile>
        {
            new("backend/src/main/java/com/mobilebank/security/JwtTokenProvider.java", "java",
                "public class JwtTokenProvider { public String generateToken() { return \"t\"; } }"),
            new("backend/src/main/java/com/mobilebank/service/JwtService.java", "java",
                "public class JwtService { public String generateToken() { return \"t\"; } }"),
            new("backend/src/main/java/com/generated/auth/JwtUtil.java", "java",
                "public class JwtUtil { public String generateToken() { return \"t\"; } }"),
        };

        JwtStackNormalizer.Normalize(files, plan).Should().BeGreaterThan(0);
        files.Should().ContainSingle(f => f.RelativePath.Contains("JwtTokenProvider", StringComparison.OrdinalIgnoreCase));
    }
}
