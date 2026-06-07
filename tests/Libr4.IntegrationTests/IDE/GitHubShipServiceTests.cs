using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;
using Libr4.IDE.Application.AutonomousAppGeneration.Verify;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class GitHubShipServiceTests
{
    [Fact]
    public void BuildHeadBranch_UsesRunIdLowercase()
    {
        var runId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
        GitHubShipService.BuildHeadBranch(runId)
            .Should().Be("libr4/autogen-a1b2c3d4e5f67890abcdef1234567890");
    }

    [Fact]
    public async Task ShipAsync_WhenDisabled_ReturnsSkipped()
    {
        var service = CreateService(new FakeGitHubApiClient(), enabled: false);
        var context = CreateContext(verifyPassed: true);

        var result = await service.ShipAsync(context);

        result.Skipped.Should().BeTrue();
        result.Summary.Should().Contain("disabled");
    }

    [Fact]
    public async Task ShipAsync_WhenVerifyNotPassed_ReturnsSkipped()
    {
        var fake = new FakeGitHubApiClient();
        var service = CreateService(fake, enabled: true);
        var context = CreateContext(verifyPassed: false);

        var result = await service.ShipAsync(context);

        result.Skipped.Should().BeTrue();
        fake.DispatchCount.Should().Be(0);
    }

    [Fact]
    public async Task ShipAsync_WhenEnabled_DispatchesWorkflowAndCreatesPr()
    {
        var fake = new FakeGitHubApiClient();
        var service = CreateService(fake, enabled: true);
        var context = CreateContext(verifyPassed: true);
        context.Files.Add(new GeneratedFile("README.md", "markdown", "# Demo"));

        var result = await service.ShipAsync(context);

        result.Success.Should().BeTrue();
        result.Skipped.Should().BeFalse();
        fake.DispatchCount.Should().Be(1);
        fake.PullRequestCount.Should().Be(1);
        result.PullRequestNumber.Should().Be(42);
        result.PullRequestUrl.Should().Contain("github.com");
    }

    [Fact]
    public async Task ShipAsync_WithObscuraEvidence_PostsManifestComment()
    {
        var fake = new FakeGitHubApiClient();
        var runsRoot = Path.Combine(Path.GetTempPath(), $"obscura-pr-{Guid.NewGuid():N}");
        var obscura = new FileSystemObscuraEvidenceStore(
            Options.Create(new ObscuraEvidenceStoreOptions { RunsRoot = runsRoot }),
            NullLogger<FileSystemObscuraEvidenceStore>.Instance);
        var service = CreateService(fake, enabled: true, obscura);
        var context = CreateContext(verifyPassed: true);
        context.Files.Add(new GeneratedFile("README.md", "markdown", "# Demo"));

        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x01, 0x02, 0x03, 0x04 };
        await obscura.PersistAsync(
            context.Orchestrator.Id,
            ObscuraEvidenceKind.Screenshot,
            png,
            new ObscuraEvidencePersistOptions(LogicalName: "verify-home", StepNumber: 1, ToolName: "browser_screenshot"));

        await service.ShipAsync(context);

        fake.PullRequestCommentCount.Should().Be(1);
        fake.LastPullRequestComment.Should().Contain("Obscura verify evidence");
        fake.LastPullRequestComment.Should().Contain("verify-home");

        try
        {
            if (Directory.Exists(runsRoot))
                Directory.Delete(runsRoot, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    private static GitHubShipService CreateService(
        FakeGitHubApiClient client,
        bool enabled,
        IObscuraEvidenceStore? obscura = null)
    {
        var options = Options.Create(new GitHubActionsDispatchOptions
        {
            Enabled = enabled,
            Owner = "libr4",
            Repository = "demo",
            PersonalAccessToken = "test-token",
            DispatchWorkflow = true,
            CreatePullRequest = true,
            AttachObscuraManifestComment = true,
            PublicApiBaseUrl = "https://ide.example.com"
        });

        return new GitHubShipService(options, client, NullLogger<GitHubShipService>.Instance, obscura);
    }

    private static GenerationContext CreateContext(bool verifyPassed)
    {
        var orchestrator = AppGenerationOrchestrator.Create("build demo app", "fp-demo");
        orchestrator.AttachPlan(new GenerationPlan(
            applicationName: "DemoApp",
            applicationDescription: "Demo",
            techStack: new TechStack(
                new[] { "C#" },
                new[] { "ASP.NET" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "dotnet"),
            phases: Array.Empty<GenerationPhase>(),
            requiredAgents: Array.Empty<string>(),
            runtimeImage: "mcr.microsoft.com/dotnet/sdk:8.0",
            buildCommands: new[] { "dotnet build" },
            testCommands: new[] { "dotnet test" }));

        var context = new GenerationContext
        {
            Orchestrator = orchestrator,
            UserRequest = orchestrator.UserRequest,
            Plan = orchestrator.Plan
        };
        context.Items["verify_passed"] = verifyPassed;
        return context;
    }

    private sealed class FakeGitHubApiClient : IGitHubApiClient
    {
        public int DispatchCount { get; private set; }
        public int PullRequestCount { get; private set; }
        public int PullRequestCommentCount { get; private set; }
        public string? LastPullRequestComment { get; private set; }

        public Task DispatchWorkflowAsync(GitHubWorkflowDispatchRequest request, CancellationToken ct = default)
        {
            DispatchCount++;
            request.Inputs.Should().ContainKey("run_id");
            return Task.CompletedTask;
        }

        public Task<GitHubPullRequestCreateResult> CreatePullRequestWithFilesAsync(
            GitHubPullRequestRequest request,
            CancellationToken ct = default)
        {
            PullRequestCount++;
            request.Files.Should().NotBeEmpty();
            return Task.FromResult(new GitHubPullRequestCreateResult(
                42,
                "https://github.com/libr4/demo/pull/42",
                request.HeadBranch));
        }

        public Task<string?> TryFetchWorkflowRunLogExcerptAsync(long runId, int maxChars, CancellationToken ct = default) =>
            Task.FromResult<string?>(null);

        public Task CreatePullRequestCommentAsync(
            GitHubRepositoryRef repository,
            int pullRequestNumber,
            string body,
            CancellationToken ct = default)
        {
            PullRequestCommentCount++;
            LastPullRequestComment = body;
            return Task.CompletedTask;
        }
    }
}
