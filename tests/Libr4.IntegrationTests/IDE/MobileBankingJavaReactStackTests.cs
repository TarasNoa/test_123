using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class MobileBankingJavaReactStackTests
{
    /// <summary>Тот же текст, что пользователь вводит в IDE-чат (см. AppGenerationChatIntentDetector).</summary>
    public const string MobileBankingJavaReactPromptRu =
        """
        Сгенерируй приложение мобильного банкинга (переводы, платежи, счета, безопасность).
        Инфраструктуру и базу данных выбери сам.
        Бэкенд на Java (Spring Boot). Фронтенд на React TypeScript.
        """;

    [Fact]
    public void ChatIntent_DetectsRussianMobileBankingPrompt()
    {
        AppGenerationChatIntentDetector.IsAppGenerationRequest(MobileBankingJavaReactPromptRu)
            .Should().BeTrue();
        StackPlanHeuristics.RequestsJavaBackendWithReactTypeScriptFrontend(MobileBankingJavaReactPromptRu)
            .Should().BeTrue();
    }

    [Fact]
    public void AlignJavaReact_OverridesDotNetPlannerOutput()
    {
        var dotnetPlan = new GenerationPlan(
            "MobileBank",
            "Generic API",
            new TechStack(
                new[] { "C#" },
                new[] { "ASP.NET Core" },
                new[] { "PostgreSQL" },
                Array.Empty<string>(),
                "planner default"),
            Array.Empty<GenerationPhase>(),
            new[] { "CodeGenerationAgent" },
            "mcr.microsoft.com/dotnet/sdk:8.0",
            new[] { "dotnet build" },
            new[] { "dotnet test" },
            4);

        var aligned = StackPlanHeuristics.AlignJavaReactFullStackPlan(dotnetPlan, MobileBankingJavaReactPromptRu);

        aligned.TechStack.Languages.Should().Contain(l => l.Contains("Java", StringComparison.OrdinalIgnoreCase));
        aligned.TechStack.Languages.Should().Contain(l => l.Contains("TypeScript", StringComparison.OrdinalIgnoreCase));
        aligned.TechStack.Frameworks.Should().Contain(f => f.Contains("Spring Boot", StringComparison.OrdinalIgnoreCase));
        aligned.TechStack.Frameworks.Should().Contain(f => f.Contains("React", StringComparison.OrdinalIgnoreCase));
        aligned.RuntimeImage.Should().NotBeNullOrEmpty().And.Match("*temurin*", because: "Java stack uses JDK runtime image");
        aligned.BuildCommands.Should().Contain(c => c.Contains("mvn", StringComparison.OrdinalIgnoreCase));
        aligned.BuildCommands.Should().Contain(c => c.Contains("npm", StringComparison.OrdinalIgnoreCase));
        aligned.ApplicationDescription.Should().Contain("[[JAVA_REACT_FULLSTACK]]");
        StackPlanHeuristics.Classify(aligned).Should().Be(StackKind.JavaReactFullStack);
    }

    [Fact]
    public void BankingSanitizer_StripsBootstrapContracts_AndForcesJavaRuntime()
    {
        var polluted = new GenerationPlan(
            "MobileBank",
            "[REPO_BOOTSTRAP_CONTEXT]{\"repository\":\"roovo/obsidian-card-board\"}[/REPO_BOOTSTRAP_CONTEXT]",
            new TechStack(
                new[] { "Java", "TypeScript" },
                new[] { "Spring Boot", "React" },
                new[] { "PostgreSQL" },
                Array.Empty<string>(),
                "polluted"),
            new[]
            {
                new GenerationPhase(1, "Repo Bootstrap Adaptation", "Adapt upstream kanban repo", Array.Empty<AgentAssignment>())
            },
            Array.Empty<string>(),
            "node:22-alpine",
            new[] { "npm install" },
            new[] { "npm test" },
            4);

        var clean = StackPlanSanitizer.Sanitize(
            StackPlanHeuristics.AlignJavaReactFullStackPlan(polluted, MobileBankingJavaReactPromptRu),
            MobileBankingJavaReactPromptRu);

        clean.ApplicationDescription.Should().NotContain("REPO_BOOTSTRAP_CONTEXT");
        clean.RuntimeImage.Should().Contain("temurin", because: "banking must not run in node-only shadow image");
        clean.BuildCommands.Should().Contain(c => c.Contains("mvn", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RepairGeneratedFile_SplitsContentEmbeddedInRelativePath()
    {
        var malformed = new GeneratedFile(
            """tests/AccountServiceIntegrationTests.cs\n\ Description: Integration tests\n\nusing Xunit;\n\npublic class AccountServiceIntegrationTests { }""",
            "csharp",
            string.Empty);

        var repaired = StackArtifactCompleteness.RepairGeneratedFile(malformed);

        repaired.Should().NotBeNull();
        repaired!.RelativePath.Should().Be("tests/AccountServiceIntegrationTests.cs");
        repaired.Content.Should().Contain("using Xunit");
        StackArtifactCompleteness.IsPlausibleFilePath(repaired.RelativePath).Should().BeTrue();
    }

    [Fact]
    public void RepairGeneratedFile_SplitsSlashNPayload()
    {
        var malformed = new GeneratedFile(
            "src/Models/Account.cs/n// Description: entity\n\nnamespace MobileBankingApp.Models;",
            "csharp",
            "");

        var repaired = StackArtifactCompleteness.RepairGeneratedFile(malformed);

        repaired.Should().NotBeNull();
        repaired!.RelativePath.Should().Be("src/Models/Account.cs");
        repaired.Content.Should().Contain("namespace MobileBankingApp.Models");
    }

    [Fact]
    public void ValidateOutputContract_ParsesPlannerJsonAfterThinkingWithBracketNotation()
    {
        var raw = """
                  <thinking>
                  5. Build commands: ["./mvnw clean package -DskipTests"]
                  </thinking>
                  {
                    "applicationName": "MobileBankApp",
                    "description": "banking",
                    "techStack": {
                      "languages": ["Java", "TypeScript"],
                      "frameworks": ["Spring Boot", "React"],
                      "databases": ["PostgreSQL"],
                      "infrastructure": ["Docker"],
                      "rationale": "java react"
                    },
                    "runtimeImage": "eclipse-temurin:21-jdk",
                    "buildCommands": ["./mvnw clean package -DskipTests"],
                    "testCommands": ["./mvnw test"],
                    "requiredAgents": ["CodeGenerationAgent", "CodeReviewAgent", "SecurityTestingAgent"],
                    "phases": [
                      {
                        "order": 1,
                        "name": "Scaffold",
                        "description": "setup",
                        "assignments": [
                          { "agentName": "CodeGenerationAgent", "role": "worker", "taskDescription": "scaffold" }
                        ]
                      }
                    ],
                    "maxIterations": 20
                  }
                  """;

        PromptPipelinePolicy.ValidateOutputContract("planning", raw, out var reason)
            .Should().BeTrue(because: reason);
    }

    [Fact]
    public void PlanCommandValidator_NormalizesMvnwWithoutCdBackend()
    {
        var validator = new DefaultPlanCommandValidator();
        var plan = new GenerationPlan(
            "MobileBankApp",
            "banking",
            new TechStack(
                new[] { "Java", "TypeScript" },
                new[] { "Spring Boot", "React" },
                new[] { "PostgreSQL" },
                Array.Empty<string>(),
                "test"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "eclipse-temurin:21-jdk",
            new[] { "./mvnw clean package -DskipTests", "npm ci && npm run build" },
            new[] { "./mvnw test", "npm test" },
            20);

        var normalized = validator.EnsureValidOrThrow(plan);

        normalized.BuildCommands.Should().Contain(c => c.Contains("cd backend", StringComparison.OrdinalIgnoreCase));
        normalized.BuildCommands.Should().Contain(c => c.Contains("cd frontend", StringComparison.OrdinalIgnoreCase));
    }

}
