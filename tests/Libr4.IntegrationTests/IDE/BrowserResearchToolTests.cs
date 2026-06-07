using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class BrowserResearchToolTests
{
    [Fact]
    public void BrowserUrlClassifier_Localhost_IsNotExternal()
    {
        BrowserUrlClassifier.IsLocalOrInternalHost("http://localhost:5173/").Should().BeTrue();
        BrowserUrlClassifier.IsLocalOrInternalHost("http://127.0.0.1:8080").Should().BeTrue();
        BrowserUrlClassifier.RequiresStealthMode("https://example.com").Should().BeTrue();
        BrowserUrlClassifier.RequiresStealthMode("http://localhost:3000").Should().BeFalse();
    }

    [Fact]
    public void BrowserUrlClassifier_ExtractsUrls_FromText()
    {
        var urls = BrowserUrlClassifier.ExtractHttpUrls(
            "Adapt https://github.com/acme/repo and see https://example.com/docs");
        urls.Should().HaveCount(2);
        urls[0].Should().Contain("github.com");
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://localhost:3000", false)]
    public void BrowserResearchTool_ResolveStealthMode_ForSources(string url, bool expectedStealth)
    {
        var input = JsonDocument.Parse($$"""{ "sources": ["{{url}}"] }""").RootElement;
        BrowserUrlClassifier.ResolveStealthMode(input, [url]).Should().Be(expectedStealth);
    }

    [Fact]
    public async Task BrowserResearchTool_ExecutesMultiUrlResearch()
    {
        var stub = new StubAgentObscuraTool();
        var tool = new BrowserResearchTool(stub);
        var input = JsonDocument.Parse("""
            {
              "query": "django solidjs",
              "sources": ["https://example.com", "http://localhost:5173"]
            }
            """).RootElement;

        var result = await tool.ExecuteAsync(input, BuildContext(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("successful=2/2");
        result.Output.Should().Contain("stealth_mode=True");
        stub.LastOptions!.StealthMode.Should().BeTrue();
        stub.LastSources.Should().BeEquivalentTo(["https://example.com", "http://localhost:5173"]);
    }

    [Fact]
    public async Task CascadeWebPrefetchService_ReturnsSummary_WhenUrlsPresent()
    {
        var stub = new StubAgentObscuraTool();
        var service = new CascadeWebPrefetchService(stub, NullLogger<CascadeWebPrefetchService>.Instance);
        var request = "Bootstrap from https://github.com/acme/calorie-app with SolidJS UI";

        var summary = await service.BuildPrefetchContextAsync(request, maxChars: 600, CancellationToken.None);

        summary.Should().NotBeNullOrWhiteSpace();
        summary.Should().Contain("browser_research");
        summary.Should().Contain("cascade_prefetch");
        summary.Should().Contain("stealth_mode");
        stub.LastOptions!.StealthMode.Should().BeTrue();
    }

    [Fact]
    public async Task CascadeWebPrefetchService_ReturnsNull_WhenNoUrls()
    {
        var stub = new StubAgentObscuraTool();
        var service = new CascadeWebPrefetchService(stub, NullLogger<CascadeWebPrefetchService>.Instance);

        var summary = await service.BuildPrefetchContextAsync("build a todo app", maxChars: 600, CancellationToken.None);

        summary.Should().BeNull();
        stub.CallCount.Should().Be(0);
    }

    private static ToolContext BuildContext()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "libr4-research-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return new ToolContext
        {
            Workspace = new ShadowWorkspaceContext(Guid.NewGuid(), tempDir, string.Empty, new StubRuntimeSession()),
            Accessor = null!,
            WorkingFiles = new List<GeneratedFile>(),
            FileState = new FileStateCache(),
            Plan = null,
            Mode = AgentSessionMode.Repair,
            Session = new AgentSessionState { RunId = Guid.NewGuid() },
            ToolInput = JsonDocument.Parse("{}").RootElement
        };
    }

    private sealed class StubAgentObscuraTool : IAgentObscuraTool
    {
        public int CallCount { get; private set; }
        public string[]? LastSources { get; private set; }
        public WebResearchOptions? LastOptions { get; private set; }

        public Task<WebResearchResult> ResearchAsync(
            string query,
            string[] sources,
            WebResearchOptions? options = null,
            CancellationToken ct = default)
        {
            CallCount++;
            LastSources = sources;
            LastOptions = options;
            return Task.FromResult(new WebResearchResult
            {
                Query = query,
                TotalSourcesChecked = sources.Length,
                SuccessfulSources = sources.Length,
                Sources = sources.Select(url => new WebSourceResult
                {
                    Url = url,
                    Title = "Title",
                    Content = $"content for {url}",
                    HasContent = true,
                    RelevanceScore = 1.0
                }).ToList()
            });
        }

        public Task<ScrapeResult> ScrapeAsync(string url, ScrapeOptions? options = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<ActionResult> PerformActionsAsync(string startUrl, BrowserAction[] actions, ActionOptions? options = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<ScreenshotResult> ScreenshotAsync(string url, ScreenshotOptions? options = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<ExtractionResult> ExtractAsync(string url, string[] extractionScripts, ExtractionOptions? options = null, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task CloseAllSessionsAsync() => Task.CompletedTask;
    }

    private sealed class StubRuntimeSession : IRuntimeSession
    {
        public string ProviderName => "stub";
        public string SessionId => "stub";
        public string HostMountPath => string.Empty;
        public string GuestMountPath => "/workspace";
        public string Image => "stub";
        public Task<ExecResult> ExecAsync(
            string command,
            string workingSubDirectory,
            IDictionary<string, string>? environmentVariables = null,
            TimeSpan? timeout = null,
            CancellationToken ct = default) =>
            Task.FromResult(new ExecResult(0, TimeSpan.Zero, Array.Empty<ConsoleLogEntry>()));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
