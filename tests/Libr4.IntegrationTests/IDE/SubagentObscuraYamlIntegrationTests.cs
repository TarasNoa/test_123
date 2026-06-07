using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class SubagentObscuraYamlIntegrationTests
{
    [Fact]
    public async Task ExecuteBrowserTask_RunsPriceMonitorYamlTaskOnStubBrowser()
    {
        var path = ResolveSpecPath("price-monitor.agent.yaml");
        var doc = AgentSpecLoader.LoadFromFile(path);
        var config = SubagentBrowserConfigMapper.Map(doc.Name, doc.Browser!);

        var browser = new YamlTaskStubBrowser();
        var selectorEngine = new DataSelectorExecutionEngine(browser, NullLogger<DataSelectorExecutionEngine>.Instance);
        var agentTool = new Mock<IAgentObscuraTool>();
        var integration = new SubagentObscuraIntegration(
            browser,
            agentTool.Object,
            selectorEngine,
            NullLogger<SubagentObscuraIntegration>.Instance);
        integration.RegisterSubagentBrowserConfig(config.SubagentId, config);

        var result = await integration.ExecuteBrowserTaskAsync(
            "price-monitor",
            "check-price",
            new Dictionary<string, string>
            {
                ["url"] = "https://shop.example.com/product/1",
                ["id"] = "sku-42"
            });

        result.Success.Should().BeTrue();
        result.TaskName.Should().Be("check-price");
        browser.NavigatedUrls.Should().Contain("https://shop.example.com/product/1");
        result.ExtractedData.Should().ContainKey("price");
    }

    private sealed class YamlTaskStubBrowser : IObscuraBrowserService
    {
        public List<string> NavigatedUrls { get; } = [];
        public List<string> ClickedSelectors { get; } = [];

        public Task<string> LaunchBrowserAsync(CancellationToken ct = default) =>
            Task.FromResult("yaml-task-session");

        public Task<string> LaunchBrowserAsync(ObscuraLaunchOptions options, CancellationToken ct = default) =>
            Task.FromResult("yaml-task-session");

        public Task NavigateAsync(string sessionId, string url, CancellationToken ct = default)
        {
            NavigatedUrls.Add(url);
            return Task.CompletedTask;
        }

        public Task<byte[]> TakeScreenshotAsync(string sessionId, CancellationToken ct = default) =>
            Task.FromResult<byte[]>([0x89, 0x50, 0x4E, 0x47]);

        public Task<string> ExecuteJavaScriptAsync(string sessionId, string script, CancellationToken ct = default) =>
            Task.FromResult("99.99");

        public Task<string> GetPageContentAsync(string sessionId, CancellationToken ct = default) =>
            Task.FromResult("<html></html>");

        public Task ClickAsync(string sessionId, string selector, CancellationToken ct = default)
        {
            ClickedSelectors.Add(selector);
            return Task.CompletedTask;
        }

        public Task TypeAsync(string sessionId, string selector, string text, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task WaitForElementAsync(string sessionId, string selector, int timeoutMs = 5000, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task CloseBrowserAsync(string sessionId, CancellationToken ct = default) => Task.CompletedTask;

        public Task<ObscuraSessionInfo?> GetSessionInfoAsync(string sessionId) =>
            Task.FromResult<ObscuraSessionInfo?>(new ObscuraSessionInfo { SessionId = sessionId, IsActive = true });

        public Task<IReadOnlyList<ObscuraSessionInfo>> ListActiveSessionsAsync() =>
            Task.FromResult<IReadOnlyList<ObscuraSessionInfo>>(Array.Empty<ObscuraSessionInfo>());

        public Task<AgentBrowserResult> ExecuteAgentTaskAsync(string sessionId, AgentBrowserTask task, CancellationToken ct = default) =>
            Task.FromResult(new AgentBrowserResult { TaskId = task.TaskId, Success = true });
    }

    [Fact]
    public void BrowserTaskTemplateEngine_SubstitutesUrlAndParams()
    {
        BrowserTaskTemplateEngine.Apply("{{url}}/items/{{id}}", new Dictionary<string, string>
        {
            ["url"] = "http://localhost:5173",
            ["id"] = "42"
        }).Should().Be("http://localhost:5173/items/42");

        var actions = BrowserTaskTemplateEngine.BuildActions(
        [
            new BrowserActionTemplate
            {
                Type = BrowserActionType.Navigate,
                Value = "{{url}}"
            },
            new BrowserActionTemplate
            {
                Type = BrowserActionType.Click,
                Selector = "button[data-id='{{id}}']"
            }
        ], new Dictionary<string, string>
        {
            ["url"] = "https://shop.example.com",
            ["id"] = "sku-1"
        });

        actions.Should().HaveCount(2);
        actions[0].Value.Should().Be("https://shop.example.com");
        actions[1].Selector.Should().Be("button[data-id='sku-1']");
    }

    [Fact]
    public void AgentSpecLoader_MergesBrowserSectionOnExtend()
    {
        var parent = new AgentSpecDocument
        {
            Name = "verify",
            Toolset = ["browser_navigate"],
            Browser = new AgentSpecBrowserSection
            {
                Enabled = true,
                StealthMode = true
            }
        };
        var child = new AgentSpecDocument
        {
            Name = "verify-calorie",
            Extend = "verify",
            Browser = new AgentSpecBrowserSection
            {
                Enabled = true,
                StealthMode = false,
                Tasks =
                [
                    new AgentSpecBrowserTask
                    {
                        TaskName = "calorie-smoke",
                        Actions =
                        [
                            new AgentSpecBrowserAction { Type = "navigate", Value = "{{url}}" }
                        ]
                    }
                ]
            }
        };

        var byName = new Dictionary<string, AgentSpecDocument>(StringComparer.OrdinalIgnoreCase)
        {
            [parent.Name] = parent,
            [child.Name] = child
        };

        var merged = AgentSpecLoader.GetMergedDocument(child, byName);
        merged.Browser.Should().NotBeNull();
        merged.Browser!.StealthMode.Should().BeFalse();
        merged.Browser.Tasks.Should().ContainSingle(t => t.TaskName == "calorie-smoke");
        merged.Toolset.Should().Contain("browser_navigate");
    }

    [Fact]
    public void SubagentBrowserConfigMapper_MapsPriceMonitorYaml()
    {
        var path = ResolveSpecPath("price-monitor.agent.yaml");
        File.Exists(path).Should().BeTrue($"spec not found: {path}");

        var doc = AgentSpecLoader.LoadFromFile(path);
        var config = SubagentBrowserConfigMapper.Map(doc.Name, doc.Browser!);

        config.SubagentId.Should().Be("price-monitor");
        config.DataSelectors.Should().HaveCount(4);
        config.DataSelectors.Should().Contain(s => s.Name == "current_price");
        config.Tasks.Should().ContainSingle(t => t.TaskName == "check-price");
        config.Tasks[0].Actions[0].Value.Should().Be("{{url}}");
        config.Tasks[0].ExtractionRules.Should().Contain(r => r.FieldName == "currency");
    }

    [Fact]
    public void SubagentBrowserConfigMapper_MapsVerifyCalorieYaml()
    {
        var path = ResolveSpecPath("verify-calorie.agent.yaml");
        File.Exists(path).Should().BeTrue($"spec not found: {path}");

        var doc = AgentSpecLoader.LoadFromFile(path);
        var merged = AgentSpecLoader.GetMergedDocument(doc, new Dictionary<string, AgentSpecDocument>(StringComparer.OrdinalIgnoreCase)
        {
            ["verify"] = AgentSpecLoader.LoadFromFile(ResolveSpecPath("verify.agent.yaml")),
            [doc.Name] = doc
        });

        var config = SubagentBrowserConfigMapper.Map(merged.Name, merged.Browser!);
        config.Tasks.Should().ContainSingle(t => t.TaskName == "calorie-smoke");
        config.DefaultViewport.Should().Be((1280, 720));
    }

    [Fact]
    public void AgentSpecRegistry_RegistersBrowserConfigFromYaml()
    {
        var specsDir = ResolveSpecsDirectory();
        var obscura = new Mock<ISubagentObscuraIntegration>();
        var registered = new Dictionary<string, SubagentBrowserConfig>(StringComparer.OrdinalIgnoreCase);
        obscura
            .Setup(x => x.RegisterSubagentBrowserConfig(It.IsAny<string>(), It.IsAny<SubagentBrowserConfig>()))
            .Callback<string, SubagentBrowserConfig>((id, cfg) => registered[id] = cfg);

        _ = new AgentSpecRegistry(
            Options.Create(new AgentSpecOptions { SpecsDirectory = specsDir }),
            NullLogger<AgentSpecRegistry>.Instance,
            obscura.Object);

        registered.Should().ContainKey("price-monitor");
        registered.Should().ContainKey("verify-calorie");
        registered["price-monitor"].Tasks.Should().ContainSingle(t => t.TaskName == "check-price");
        registered["verify-calorie"].Tasks.Should().ContainSingle(t => t.TaskName == "calorie-smoke");
        obscura.Verify(
            x => x.RegisterSubagentBrowserConfig(It.IsAny<string>(), It.IsAny<SubagentBrowserConfig>()),
            Times.AtLeast(2));
    }

    private static string ResolveSpecsDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Agents", "Subagents"),
            Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "src", "Services", "IDE", "Libr4.IDE.AutonomousAppGeneration", "Agents", "Subagents"))
        };

        return candidates.First(Directory.Exists);
    }

    private static string ResolveSpecPath(string fileName) =>
        Path.Combine(ResolveSpecsDirectory(), fileName);
}
