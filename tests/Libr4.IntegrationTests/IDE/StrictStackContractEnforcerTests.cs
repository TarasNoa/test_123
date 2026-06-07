using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class StrictStackContractEnforcerTests
{
    private const string CalorieRequest =
        "Строго Django backend + SolidJS frontend (backend/ + frontend/). " +
        "Не использовать React, Vue, NestJS. TypeScript + Python. " +
        "Калькулятор калорий по фото с OpenAI gpt-4o Vision.";

    [Fact]
    public void Enforce_LocksDjangoSolidJs_FromExplicitRequest()
    {
        var plan = new GenerationPlan(
            "CalorieVision",
            "Calorie tracker app",
            new TechStack(
                new[] { "TypeScript" },
                new[] { "Nuxt", "Vue" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "wrong stack"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "node:22-alpine",
            new[] { "npm ci" },
            new[] { "npm test" },
            5);

        var enforced = StrictStackContractEnforcer.Enforce(plan, CalorieRequest);

        enforced.TechStack.Languages.Should().Contain(new[] { "Python", "TypeScript" });
        enforced.TechStack.Languages.Should().NotContain("Go");
        enforced.TechStack.Frameworks.Should().Contain("Django");
        enforced.TechStack.Frameworks.Should().Contain("SolidJS");
        enforced.TechStack.Frameworks.Should().NotContain(f => f.Contains("Vue") || f.Contains("Nuxt") || f.Contains("React"));
        enforced.ApplicationDescription.Should().Contain(StrictStackContractEnforcer.ContractMarker);
        enforced.RuntimeImage.Should().Contain("python");
        enforced.BuildCommands.Should().Contain(c => c.Contains("manage.py"));
        enforced.BuildCommands.Should().Contain(c => c.Contains("frontend") && c.Contains("npm"));
    }

    [Fact]
    public void GoldenStackPlanAligner_PreservesStack_WhenStrictContractActive()
    {
        var enforced = StrictStackContractEnforcer.Enforce(
            new GenerationPlan(
                "CalorieVision",
                "Django API + SolidJS UI",
                new TechStack(
                    new[] { "Python", "TypeScript" },
                    new[] { "Django", "SolidJS", "Vite" },
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    "user stack"),
                Array.Empty<GenerationPhase>(),
                Array.Empty<string>(),
                "python:3.12-slim",
                Array.Empty<string>(),
                Array.Empty<string>(),
                5),
            CalorieRequest);

        var aligned = GoldenStackPlanAligner.Align(enforced, CalorieRequest);

        aligned.TechStack.Languages.Should().Contain(new[] { "Python", "TypeScript" });
        aligned.TechStack.Frameworks.Should().Contain("Django");
        aligned.TechStack.Frameworks.Should().Contain("SolidJS");
        aligned.TechStack.Frameworks.Should().NotContain(f => f.Contains("Vue") || f.Contains("Nuxt"));
    }

    [Fact]
    public void Parse_DjangoRequest_DoesNotAddGoLanguage()
    {
        var contract = StrictStackContractEnforcer.Parse(CalorieRequest);
        contract.Should().NotBeNull();
        contract!.Languages.Should().Contain("Python");
        contract.Languages.Should().NotContain("Go");
    }

    [Fact]
    public void StackPlanSanitizer_DoesNotSubstituteVueNuxt_ForDjangoSolidJs()
    {
        var corrupted = new GenerationPlan(
            "CalorieVision",
            "Full stack calorie app",
            new TechStack(
                new[] { "TypeScript" },
                new[] { "Nuxt", "Vue" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "llm mistake"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "node:22-alpine",
            new[] { "npm ci && npm run build" },
            new[] { "npm test" },
            5);

        var sanitized = StackPlanSanitizer.Sanitize(corrupted, CalorieRequest);

        sanitized.TechStack.Frameworks.Should().Contain("Django");
        sanitized.TechStack.Frameworks.Should().Contain("SolidJS");
        sanitized.TechStack.Frameworks.Should().NotContain(f => f.Contains("Vue") || f.Contains("Nuxt"));
    }
}
