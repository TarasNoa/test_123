using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.DiffReview;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

/// <summary>Phase 7.4.5 — contract stability for frontend diff/review API consumers.</summary>
public sealed class DiffReviewApiContractTests
{
    private static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void RunDiffListResponse_SerializesExpectedFrontendKeys()
    {
        var payload = new RunDiffListResponse(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            1,
            [
                new RunFileDiffSummary(
                    "src/App.tsx",
                    "typescript",
                    RunDiffChangeKind.Modify,
                    3,
                    "write_file",
                    1,
                    DateTime.UtcNow,
                    "rollout:12")
            ]);

        var json = JsonSerializer.Serialize(payload, CamelCase);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("runId", out _).Should().BeTrue();
        root.TryGetProperty("total", out _).Should().BeTrue();
        root.GetProperty("items")[0].TryGetProperty("stepNumber", out _).Should().BeTrue();
        root.GetProperty("items")[0].TryGetProperty("provenanceId", out _).Should().BeTrue();
    }

    [Fact]
    public void RunReviewStatusResponse_SerializesExpectedFrontendKeys()
    {
        var payload = new RunReviewStatusResponse(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            RunReviewStatus.Partial,
            true,
            2,
            1,
            1,
            0,
            0,
            [new FileReviewState("a.ts", ReviewDecision.Approve, null, "user-1", DateTime.UtcNow)],
            ["b.ts"]);

        var json = JsonSerializer.Serialize(payload, CamelCase);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("requireHumanReview", out _).Should().BeTrue();
        root.TryGetProperty("pendingPaths", out _).Should().BeTrue();
        root.GetProperty("files")[0].TryGetProperty("decision", out _).Should().BeTrue();
    }

    [Fact]
    public void FileDiffEvidenceResponse_SerializesExpectedFrontendKeys()
    {
        var payload = new FileDiffEvidenceResponse(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "frontend/App.tsx",
            5,
            [
                new DiffEvidenceItem(
                    "verify",
                    "Screenshot",
                    "screenshot-final.png",
                    "/api/ide/app-generation/x/verify/artifacts/screenshot-final.png",
                    "/api/ide/app-generation/x/verify/artifacts/screenshot-final.png",
                    null,
                    null,
                    false,
                    1024,
                    DateTime.UtcNow)
            ],
            [new DiffEvidenceOverlay("verify_console", "console_error:frontend/App.tsx", "console_error")]);

        var json = JsonSerializer.Serialize(payload, CamelCase);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("correlatedStepNumber", out _).Should().BeTrue();
        root.GetProperty("items")[0].TryGetProperty("downloadUrl", out _).Should().BeTrue();
        root.GetProperty("items")[0].TryGetProperty("stepMatched", out _).Should().BeTrue();
        root.GetProperty("overlays")[0].TryGetProperty("kind", out _).Should().BeTrue();
    }
}
