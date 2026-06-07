using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using AgentOrchestrationOptions = Libr4.IDE.AutonomousAppGeneration.Agents.AgentOrchestrationOptions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class NativeManifestSyncEnginesTests
{
    [Fact]
    public void SyncGoMod_AddsGinWhenReferencedInSource()
    {
        var files = new List<GeneratedFile>
        {
            new("go.mod", "go", "module example.com/app\n\ngo 1.22\n"),
            new("cmd/server/main.go", "go", """
                package main
                import "github.com/gin-gonic/gin"
                func main() { gin.Default() }
                """)
        };

        NativeManifestSyncEngines.SyncGoMod(files).Should().Be(1);
        files[0].Content.Should().Contain("github.com/gin-gonic/gin");
    }

    [Fact]
    public void SyncCargoToml_AddsAxumWhenUsed()
    {
        var files = new List<GeneratedFile>
        {
            new("Cargo.toml", "toml", "[package]\nname=\"api\"\nversion=\"0.1.0\"\n"),
            new("src/main.rs", "rust", "use axum::Router;\nfn main() {}")
        };

        NativeManifestSyncEngines.SyncCargoToml(files).Should().Be(1);
        files[0].Content.Should().Contain("axum");
    }

    [Fact]
    public void BenchmarkResolver_ForcesZeroLlmReviewRounds()
    {
        var baseline = new AgentOrchestrationOptions { MaxLlmReviewRounds = 2 };
        var benchmark = new AutonomousBenchmarkModeOptions
        {
            EnableBenchmarkMode = true,
            SkipMultiAgentLlmReview = true
        };

        var resolved = BenchmarkOrchestrationOptionsResolver.Resolve(baseline, benchmark);
        resolved.MaxLlmReviewRounds.Should().Be(0);
        resolved.ExcludeInfrastructurePhases.Should().BeTrue();
    }
}
