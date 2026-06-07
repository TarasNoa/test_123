using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BuildDiagnosticsDashboardServiceTests
{
    [Fact]
    public void Build_GroupsGatesIntoPhasesAndTimeline()
    {
        var orchestrator = AppGenerationOrchestrator.Create("Bank app", "fingerprint-bank-test");
        orchestrator.RecordQualityGate("pre_safety_normalization", 9, true, new[] { "jwt:ok" });
        orchestrator.RecordQualityGate("build:stack_safety_net", 4, false, new[] { "maven_failed" });
        orchestrator.RecordQualityGate("repair_error_classifier", 10, true, new[] { "PomSyntax:L0" });
        orchestrator.RecordQualityGate("runtime_recovery_l3", 8, true, new[] { "patches=2" });

        var evidenceStore = new FileSystemVerifyEvidenceStore(
            Options.Create(new VerifySubagentOptions { EvidenceRoot = Path.Combine(Path.GetTempPath(), $"dash-{Guid.NewGuid():N}") }),
            NullLogger<FileSystemVerifyEvidenceStore>.Instance);
        var service = new BuildDiagnosticsDashboardService(evidenceStore);
        var quality = new RunQualityAssessmentService().Assess(orchestrator);
        var dashboard = service.Build(orchestrator, quality);

        dashboard.Timeline.Should().HaveCount(4);
        dashboard.Phases.Should().Contain(p => p.Category == "normalization");
        dashboard.Phases.Should().Contain(p => p.Category == "build");
        dashboard.RepairTiers.Should().NotBeEmpty();
        dashboard.Recommendations.Should().NotBeEmpty();
        dashboard.Summary.FailedGates.Should().Be(1);
        dashboard.Summary.DetectedStack.Should().Be("Unknown");
        dashboard.Summary.CatalogLanguageCount.Should().BeGreaterThanOrEqualTo(50);
        dashboard.Summary.CatalogFrameworkCount.Should().BeGreaterThanOrEqualTo(30);
        dashboard.RecoveryEfficiency.TotalAttempts.Should().Be(0);
        dashboard.RecoveryEfficiency.Insight.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Build_IncludesVerifyEvidenceArtifacts()
    {
        var orchestrator = AppGenerationOrchestrator.Create("Verify app", "fp-verify");
        var evidenceRoot = Path.Combine(Path.GetTempPath(), $"dash-verify-{Guid.NewGuid():N}");
        var evidenceStore = new FileSystemVerifyEvidenceStore(
            Options.Create(new VerifySubagentOptions { EvidenceRoot = evidenceRoot }),
            NullLogger<FileSystemVerifyEvidenceStore>.Instance);

        var runId = orchestrator.Id;
        var evidenceDir = evidenceStore.GetEvidenceDirectory(runId);
        Directory.CreateDirectory(evidenceDir);
        File.WriteAllText(Path.Combine(evidenceDir, "verify-report.json"), "{\"passed\":true}");

        var service = new BuildDiagnosticsDashboardService(evidenceStore);
        var dashboard = service.Build(orchestrator, new RunQualityAssessmentService().Assess(orchestrator));

        dashboard.VerifyEvidence.Should().NotBeNull();
        dashboard.VerifyEvidence!.DirectoryExists.Should().BeTrue();
        dashboard.VerifyEvidence.Artifacts.Should().Contain(a => a.FileName == "verify-report.json");

        try
        {
            if (Directory.Exists(evidenceRoot))
                Directory.Delete(evidenceRoot, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
