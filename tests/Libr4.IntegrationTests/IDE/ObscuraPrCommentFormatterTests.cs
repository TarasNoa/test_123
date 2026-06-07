using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Application.Obscura;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ObscuraPrCommentFormatterTests
{
    [Fact]
    public void BuildCommentBody_IncludesArtifactTableAndScreenshotLinks()
    {
        var runId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var artifacts = new[]
        {
            new ObscuraEvidenceArtifact(
                ObscuraEvidenceKind.Screenshot,
                "abc123.png",
                "hash1",
                "obscura/abc123.png",
                "/tmp/obscura/abc123.png",
                1024,
                DateTime.UtcNow,
                "image/png",
                "/api/local",
                ThumbnailUrl: null,
                LogicalName: "verify-home",
                StepNumber: 1,
                ToolName: "browser_screenshot")
        };

        var body = ObscuraPrCommentFormatter.BuildCommentBody(
            runId,
            artifacts,
            "https://ide.example.com");

        body.Should().NotBeNullOrWhiteSpace();
        body.Should().Contain("Obscura verify evidence");
        body.Should().Contain("verify-home");
        body.Should().Contain("![verify-home]");
        body.Should().Contain("https://ide.example.com/api/v1/ide/app-generation/");
    }

    [Fact]
    public void BuildCommentBody_ReturnsNull_WhenNoArtifacts()
    {
        ObscuraPrCommentFormatter.BuildCommentBody(Guid.NewGuid(), Array.Empty<ObscuraEvidenceArtifact>())
            .Should().BeNull();
    }
}
