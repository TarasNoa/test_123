using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackRecovery.Ecosystems;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class DeveloperEcosystemCatalogTests
{
    [Fact]
    public void Catalog_ContainsAtLeast50Languages()
    {
        DeveloperEcosystemCatalog.LanguageCount.Should().BeGreaterThanOrEqualTo(50);
        DeveloperEcosystemCatalog.FrameworkCount.Should().BeGreaterThanOrEqualTo(60);
        DeveloperEcosystemCatalog.AllProfiles.Should().Contain(p => p.Id == "solidjs");
        DeveloperEcosystemCatalog.AllProfiles.Should().Contain(p => p.Id == "nextjs");
        DeveloperEcosystemCatalog.AllProfiles.Should().Contain(p => p.Id == "sveltekit");
        DeveloperEcosystemCatalog.AllProfiles.Should().Contain(p => p.Id == "tanstack-router");
    }

    [Fact]
    public void Matcher_DetectsNextJsAndTypeScript_FromPlan()
    {
        var plan = new GenerationPlan(
            "Shop",
            "Next.js 14 app router with TypeScript and React server components",
            new TechStack(
                new[] { "TypeScript" },
                new[] { "Next.js", "React" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "node:20"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "node:20",
            Array.Empty<string>(),
            Array.Empty<string>(),
            5);

        var files = new List<GeneratedFile>
        {
            new("package.json", "json", """{ "dependencies": { "next": "14" } }"""),
            new("next.config.ts", "typescript", "export default {}"),
            new("app/page.tsx", "typescript", "export default function Page() { return null }"),
        };

        var matches = EcosystemMatcher.Match(plan, files);
        matches.Select(m => m.Profile.Id).Should().Contain("nextjs");
        matches.Select(m => m.Profile.Id).Should().Contain("typescript");
        matches.Select(m => m.Profile.Id).Should().Contain("react");
    }

    [Fact]
    public void PatternRecovery_DeduplicatesDuplicateNextAppRoutes()
    {
        var plan = StackPlanHeuristics.AlignJavaReactFullStackPlan(
            new GenerationPlan("App", "React TS", StackPlanHeuristics.CreateJavaReactFullStackTechStack(),
                Array.Empty<GenerationPhase>(), Array.Empty<string>(), "node:20",
                Array.Empty<string>(), Array.Empty<string>(), 5),
            "react typescript");

        var files = new List<GeneratedFile>
        {
            new("frontend/package.json", "json", "{}"),
            new("frontend/app/page.tsx", "tsx", "export default function A(){}"),
            new("frontend/pages/index.tsx", "tsx", "export default function B(){}"),
        };

        var warnings = new List<string>();
        PatternBasedEcosystemRecovery.Normalize(files, plan, warnings, autoFix: true);
        files.Count(f => f.RelativePath.EndsWith("page.tsx", StringComparison.OrdinalIgnoreCase)
                        || f.RelativePath.Contains("index.tsx", StringComparison.OrdinalIgnoreCase))
            .Should().Be(1);
    }

    [Fact]
    public void Matcher_DetectsGoAndGin_FromArtifacts()
    {
        var plan = new GenerationPlan(
            "Api",
            "REST API in Go",
            new TechStack(new[] { "Go" }, new[] { "Gin" }, Array.Empty<string>(), Array.Empty<string>(), "golang:1.22"),
            Array.Empty<GenerationPhase>(), Array.Empty<string>(), "golang:1.22",
            Array.Empty<string>(), Array.Empty<string>(), 5);

        var files = new List<GeneratedFile>
        {
            new("go.mod", "text", "module example.com/app\ngo 1.22\n"),
            new("main.go", "go", "func main() { r := gin.Default() }"),
            new("cmd/server/main.go", "go", "func main() { r := gin.Default() }"),
        };

        var matches = EcosystemMatcher.Match(plan, files);
        matches.Select(m => m.Profile.Id).Should().Contain("go");
        matches.Select(m => m.Profile.Id).Should().Contain("gin");
    }

    [Fact]
    public void Catalog_ContainsAll50IndustryLanguages()
    {
        var required = new[]
        {
            "java", "kotlin", "scala", "groovy", "csharp", "fsharp", "vbnet", "python",
            "javascript", "typescript", "go", "rust", "c", "cpp", "php", "ruby", "elixir",
            "erlang", "dart", "swift", "objectivec", "zig", "nim", "crystal", "ocaml",
            "haskell", "lua", "r", "julia", "perl", "clojure", "lisp", "scheme", "fortran",
            "cobol", "ada", "prolog", "apex", "abap", "solidity", "v", "dlang", "elm",
            "reasonml", "rescript", "coffeescript", "powershell", "bash", "matlab", "wolfram"
        };

        foreach (var id in required)
            DeveloperEcosystemCatalog.FindById(id).Should().NotBeNull($"missing language profile: {id}");
    }

    [Fact]
    public void TierRegistry_AssignsTier1ToGoldenPathStacks()
    {
        DeveloperEcosystemTierRegistry.GetTier("spring-boot").Should().Be(EcosystemSupportTier.Tier1);
        DeveloperEcosystemTierRegistry.GetTier("fastapi").Should().Be(EcosystemSupportTier.Tier1);
        DeveloperEcosystemTierRegistry.GetTier("nestjs").Should().Be(EcosystemSupportTier.Tier1);
        DeveloperEcosystemTierRegistry.GetTier("gin").Should().Be(EcosystemSupportTier.Tier1);
        DeveloperEcosystemTierRegistry.GetTier("laravel").Should().Be(EcosystemSupportTier.Tier1);
        DeveloperEcosystemTierRegistry.GetTier("scala").Should().Be(EcosystemSupportTier.Tier3);
        DeveloperEcosystemTierRegistry.GetTier("haskell").Should().Be(EcosystemSupportTier.Tier3);
    }

    [Fact]
    public void GoldenStackPath_DetectsFastApiReact_FromRequest()
    {
        var path = GoldenStackPathRegistry.DetectFromRequest(
            "Build a fintech API with Python FastAPI backend and React TypeScript frontend");
        path.Should().NotBeNull();
        path!.Id.Should().Be("python-fastapi-react");
        path.Tier.Should().Be(EcosystemSupportTier.Tier1);
        path.RemediationDepth.Should().Be(RemediationDepth.GoldenPath);
    }

    [Fact]
    public void GoldenStackPlanAligner_InjectsContractMarkerForNestJsReact()
    {
        var plan = new GenerationPlan(
            "Shop",
            "NestJS API with React admin panel",
            new TechStack(new[] { "TypeScript" }, new[] { "NestJS", "React" }, Array.Empty<string>(), Array.Empty<string>(), "node:20"),
            Array.Empty<GenerationPhase>(),
            Array.Empty<string>(),
            "node:20",
            Array.Empty<string>(),
            Array.Empty<string>(),
            5);

        var aligned = GoldenStackPlanAligner.Align(plan, "NestJS + React full stack");
        aligned.ApplicationDescription.Should().Contain("[[NESTJS_REACT_FULLSTACK]]");
        aligned.BuildCommands.Should().Contain(c => c.Contains("backend") && c.Contains("npm"));
        aligned.TechStack.Languages.Should().Contain("TypeScript");
    }

    [Theory]
    [InlineData("vapor", "swift")]
    [InlineData("phoenix-liveview", "elixir")]
    [InlineData("hardhat", "solidity")]
    [InlineData("yesod", "haskell")]
    public void Catalog_ContainsProductionFrameworkProfiles(string frameworkId, string languageId)
    {
        DeveloperEcosystemCatalog.FindById(frameworkId).Should().NotBeNull();
        DeveloperEcosystemCatalog.FindById(languageId).Should().NotBeNull();
    }
}
