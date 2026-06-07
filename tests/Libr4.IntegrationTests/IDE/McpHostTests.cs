using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class McpHostCatalogTests
{
    [Fact]
    public void ListTools_IncludesRegisteredMcpTools()
    {
        var catalog = new McpHostCatalog(new DefaultMcpToolRegistry());
        var tools = catalog.ListTools();

        tools.Should().Contain(t => t.Name == "browser.smoke");
        tools.Should().Contain(t => t.Name == "mcp.list");
    }

    [Fact]
    public void ListResources_IncludesInternalResources()
    {
        var catalog = new McpHostCatalog(new DefaultMcpToolRegistry());
        catalog.ListResources().Should().Contain(r => r.Uri.StartsWith("memory://"));
    }

    [Fact]
    public void RegisterDiscoveredTools_MergesIntoCatalog()
    {
        var catalog = new McpHostCatalog(new DefaultMcpToolRegistry());
        catalog.RegisterDiscoveredTools(
            "custom-server",
            McpHostTransportKind.Stdio,
            [new McpCatalogTool("custom.tool", "custom-server", McpHostTransportKind.Stdio, "demo", [])]);

        catalog.ListTools().Should().Contain(t => t.Name == "custom.tool");
    }
}

public sealed class McpRunHostManagerTests
{
    [Fact]
    public void IsUnifiedHostEnabled_RespectsOptions()
    {
        var manager = new McpRunHostManager(
            Options.Create(new McpHostOptions { EnableUnifiedHost = true }),
            Options.Create(new McpExecutionOptions()),
            new FakeDiscovery(),
            new FakeHttpClientFactory(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<McpRunHostManager>.Instance);

        manager.IsUnifiedHostEnabled.Should().BeTrue();
    }

    [Fact]
    public void ReleaseRun_RemovesTrackedSessions()
    {
        var manager = new McpRunHostManager(
            Options.Create(new McpHostOptions()),
            Options.Create(new McpExecutionOptions()),
            new FakeDiscovery(),
            new FakeHttpClientFactory(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<McpRunHostManager>.Instance);

        var runId = Guid.NewGuid();
        manager.ReleaseRun(runId);
        manager.ListActiveSessions().Where(s => s.RunId == runId).Should().BeEmpty();
    }

    private sealed class FakeDiscovery : IMcpExternalServerDiscovery
    {
        public Task<IReadOnlyList<McpServerDiscoveryResult>> DiscoverAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<McpServerDiscoveryResult>>(Array.Empty<McpServerDiscoveryResult>());
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
