using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ObscuraMcpBridgeTests
{
    [Fact]
    public void CanHandle_ReturnsTrue_ForBrowserTools_WhenProviderIsObscura()
    {
        var bridge = CreateBridge(new StubAgentObscuraTool());
        var options = new McpExecutionOptions { BrowserLane = new BrowserLaneOptions { Provider = "Obscura" } };

        bridge.CanHandle("browser.smoke", options).Should().BeTrue();
        bridge.CanHandle("browser.auth", options).Should().BeTrue();
        bridge.CanHandle("n8n.workflow.test", options).Should().BeFalse();
    }

    [Fact]
    public void CanHandle_ReturnsFalse_WhenProviderIsNode()
    {
        var bridge = CreateBridge(new StubAgentObscuraTool());
        var options = new McpExecutionOptions { BrowserLane = new BrowserLaneOptions { Provider = "Node" } };

        bridge.CanHandle("browser.smoke", options).Should().BeFalse();
    }

    [Fact]
    public async Task BrowserSmoke_RoutesToObscura_AndReturnsActionableSummary()
    {
        var stub = new StubAgentObscuraTool();
        var bridge = CreateBridge(stub);
        var args = new Dictionary<string, object?>
        {
            ["url"] = "https://github.com/search?q=todo+kanban+auth+license%3Amit",
            ["query"] = "todo kanban auth",
            ["mode"] = "repo_bootstrap_probe"
        };

        var outcome = await bridge.InvokeAsync("browser.smoke", args, runId: Guid.NewGuid());

        outcome.Succeeded.Should().BeTrue();
        outcome.OutcomeCode.Should().Be("obscura_succeeded");
        outcome.ResultSummary.Should().Contain("github.com/");
        outcome.ResultSummary.Should().Contain("license");
        outcome.ResultSummary.Should().Contain("repository_url");
        stub.LastScrapeUrl.Should().Be(args["url"] as string);
    }

    [Fact]
    public async Task BrowserAuth_UsesPerformActions_WithDefaultSelectors()
    {
        var stub = new StubAgentObscuraTool { AuthSuccess = true };
        var bridge = CreateBridge(stub);
        var args = new Dictionary<string, object?>
        {
            ["url"] = "http://localhost:5200/login",
            ["username"] = "test@example.com",
            ["password"] = "secret"
        };

        var outcome = await bridge.InvokeAsync("browser.auth", args, runId: Guid.NewGuid());

        outcome.Succeeded.Should().BeTrue();
        outcome.OutcomeCode.Should().Be("obscura_succeeded");
        stub.LastAuthActions.Should().NotBeEmpty();
        stub.LastAuthActions.Should().Contain(a => a.Type == BrowserActionType.Type);
        stub.LastAuthActions.Should().Contain(a => a.Type == BrowserActionType.Click);
    }

    [Fact]
    public async Task McpInvocationService_UsesObscuraBridge_InsteadOfNodeTransport()
    {
        var stub = new StubAgentObscuraTool();
        var bridge = new ObscuraMcpBridge(
            stub,
            Options.Create(new McpExecutionOptions
            {
                BrowserLane = new BrowserLaneOptions { Provider = "Obscura" }
            }),
            NullLogger<ObscuraMcpBridge>.Instance);

        var mcp = new McpToolInvocationService(
            new DefaultMcpToolRegistry(),
            new DefaultMcpExecutionPolicy(Options.Create(new McpExecutionPolicyOptions())),
            new DefaultMcpSessionRouter(),
            Options.Create(new McpExecutionOptions
            {
                EnableStdioTransport = true,
                BrowserLane = new BrowserLaneOptions { Provider = "Obscura" }
            }),
            new FakeMcpServerPreflight(alwaysAvailable: false),
            NullLogger<McpToolInvocationService>.Instance,
            bridge);

        var outcome = await mcp.InvokeStandaloneAsync(
            null,
            "browser.smoke",
            new Dictionary<string, object?>
            {
                ["url"] = "https://github.com/acme/demo-repo"
            },
            CancellationToken.None);

        outcome.Succeeded.Should().BeTrue();
        outcome.OutcomeCode.Should().Be("obscura_succeeded");
        stub.ScrapeCount.Should().Be(1);
    }

    private static ObscuraMcpBridge CreateBridge(IAgentObscuraTool agentTool) =>
        new(
            agentTool,
            Options.Create(new McpExecutionOptions
            {
                BrowserLane = new BrowserLaneOptions { Provider = "Obscura" },
                BrowserProfiles = new Dictionary<string, BrowserLaneProfile>(StringComparer.OrdinalIgnoreCase)
                {
                    ["smoke"] = new BrowserLaneProfile { BaseUrl = "http://localhost:5173/" },
                    ["auth"] = new BrowserLaneProfile
                    {
                        BaseUrl = "http://localhost:5200/login",
                        Environment = new Dictionary<string, string>
                        {
                            ["AUTH_TEST_USER"] = "test@example.com",
                            ["AUTH_TEST_PASS"] = "test123"
                        }
                    }
                }
            }),
            NullLogger<ObscuraMcpBridge>.Instance);

    private sealed class StubAgentObscuraTool : IAgentObscuraTool
    {
        public int ScrapeCount { get; private set; }
        public string? LastScrapeUrl { get; private set; }
        public BrowserAction[] LastAuthActions { get; private set; } = [];
        public bool AuthSuccess { get; init; } = true;

        public Task<WebResearchResult> ResearchAsync(string query, string[] sources, WebResearchOptions? options = null, CancellationToken ct = default)
            => Task.FromResult(new WebResearchResult());

        public Task<ScrapeResult> ScrapeAsync(string url, ScrapeOptions? options = null, CancellationToken ct = default)
        {
            ScrapeCount++;
            LastScrapeUrl = url;
            return Task.FromResult(new ScrapeResult
            {
                Url = url,
                Title = "GitHub Search",
                TextContent =
                    "repo https://github.com/acme/demo-repo license mit clone with git clone https://github.com/acme/demo-repo.git",
                Links =
                [
                    "https://github.com/acme/demo-repo",
                    "https://github.com/search"
                ],
                Screenshot = [0x89, 0x50, 0x4E, 0x47]
            });
        }

        public Task<ActionResult> PerformActionsAsync(
            string startUrl,
            BrowserAction[] actions,
            ActionOptions? options = null,
            CancellationToken ct = default)
        {
            LastAuthActions = actions;
            return Task.FromResult(new ActionResult
            {
                StartUrl = startUrl,
                FinalUrl = startUrl + "/dashboard",
                Success = AuthSuccess,
                Logs = ["login ok"],
                Screenshots =
                [
                    new ActionScreenshot
                    {
                        ActionIndex = actions.Length,
                        ActionType = "Final",
                        Data = [0x89, 0x50, 0x4E, 0x47]
                    }
                ]
            });
        }

        public Task<ScreenshotResult> ScreenshotAsync(string url, ScreenshotOptions? options = null, CancellationToken ct = default)
            => Task.FromResult(new ScreenshotResult { Url = url, ScreenshotData = [1, 2, 3] });

        public Task<ExtractionResult> ExtractAsync(string url, string[] extractionScripts, ExtractionOptions? options = null, CancellationToken ct = default)
            => Task.FromResult(new ExtractionResult { Url = url });

        public Task CloseAllSessionsAsync() => Task.CompletedTask;
    }
}
