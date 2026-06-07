using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class JavaPackageRootConsolidatorTests
{
    [Fact]
    public void Consolidate_DropsGeneratedBanking_WhenMobileBankProPresent()
    {
        var plan = new GenerationPlan(
            "MobileBankPro",
            "Banking",
            new Libr4.IDE.Domain.AutonomousAppGeneration.TechStack(
                new[] { "Java", "TypeScript" },
                new[] { "Spring Boot", "React" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                ""),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "eclipse-temurin:21-jdk",
            Array.Empty<string>(),
            Array.Empty<string>(),
            20);

        var files = new List<GeneratedFile>
        {
            new("backend/pom.xml", "xml", "<project/>"),
            new("backend/src/main/java/com/mobilebankpro/App.java", "java", "package com.mobilebankpro;"),
            new("backend/src/main/java/com/mobilebankpro/web/AuthController.java", "java", "package com.mobilebankpro.web;"),
            new("backend/src/main/java/com/generated/banking/BankingApplication.java", "java", "package com.generated.banking;"),
            new("frontend/package.json", "json", "{}")
        };

        var result = JavaPackageRootConsolidator.Consolidate(files, plan);

        result.Should().NotContain(f => f.RelativePath.Contains("com/generated/banking", StringComparison.OrdinalIgnoreCase));
        result.Should().Contain(f => f.RelativePath.Contains("com/mobilebankpro", StringComparison.OrdinalIgnoreCase));
    }
}
