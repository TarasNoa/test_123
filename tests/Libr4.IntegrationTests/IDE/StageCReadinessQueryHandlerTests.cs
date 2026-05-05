using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Handlers;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;
using System.IO;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class StageCReadinessQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnDegraded_WhenAnyLaneDegraded()
    {
        var watchdog = new FakeWatchdog(new[]
        {
            new McpLaneWatchdogSnapshot
            {
                ProfileKey = "browser-lane",
                Lane = "Browser",
                LastCheckTimeUtc = DateTime.UtcNow,
                Status = "degraded",
                BlockerCode = "mcp_server_missing",
                DiagnosticMessage = "missing"
            }
        });
        var registry = new FakeRegistry(new[]
        {
            new McpToolMetadata(
                "browser.smoke",
                "browser-lane",
                "desc",
                McpToolRiskLevel.High,
                McpExecutionLaneKind.Browser,
                new[] { "ui" })
        });
        var options = Options.Create(new McpExecutionOptions
        {
            EnableDeterministicFallback = true,
            EnableStdioTransport = false,
            KillSwitchBrowserLane = false
        });

        var handler = new GetStageCReadinessQueryHandler(watchdog, registry, options);
        var result = await handler.Handle(new GetStageCReadinessQuery(), CancellationToken.None);

        result.OverallStatus.Should().Be("degraded");
        result.DegradedProfiles.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.ProfileKey == "browser-lane" && i.BlockerCode == "mcp_server_missing");
    }

    [Fact]
    public async Task Handle_ShouldSetKillSwitchFlag_WhenLaneDisabled()
    {
        var watchdog = new FakeWatchdog(new[]
        {
            new McpLaneWatchdogSnapshot
            {
                ProfileKey = "n8n-lane",
                Lane = "N8n",
                LastCheckTimeUtc = DateTime.UtcNow,
                Status = "available"
            }
        });
        var registry = new FakeRegistry(new[]
        {
            new McpToolMetadata(
                "n8n.workflow.test",
                "n8n-lane",
                "desc",
                McpToolRiskLevel.High,
                McpExecutionLaneKind.N8n,
                new[] { "workflow" })
        });
        var options = Options.Create(new McpExecutionOptions
        {
            KillSwitchN8nLane = true
        });

        var handler = new GetStageCReadinessQueryHandler(watchdog, registry, options);
        var result = await handler.Handle(new GetStageCReadinessQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(i => i.ProfileKey == "n8n-lane" && i.KillSwitchActive);
    }

    [Fact]
    public void McpServerStub_BrowserLane_ShouldExistAndBeValid()
    {
        var browserServerPath = "d:/lib4_project/browser-mcp-server/server.js";
        var browserPackagePath = "d:/lib4_project/browser-mcp-server/package.json";

        File.Exists(browserServerPath).Should().BeTrue("browser-mcp-server stub should exist");
        File.Exists(browserPackagePath).Should().BeTrue("browser-mcp-server package.json should exist");

        var serverContent = File.ReadAllText(browserServerPath);
        serverContent.Should().Contain("initialize", "browser-mcp-stub", "tools/list");
    }

    [Fact]
    public void McpServerStub_N8nLane_ShouldExistAndBeValid()
    {
        var n8nServerPath = "d:/lib4_project/n8n-mcp-server/server.js";
        var n8nPackagePath = "d:/lib4_project/n8n-mcp-server/package.json";

        File.Exists(n8nServerPath).Should().BeTrue("n8n-mcp-server stub should exist");
        File.Exists(n8nPackagePath).Should().BeTrue("n8n-mcp-server package.json should exist");

        var serverContent = File.ReadAllText(n8nServerPath);
        serverContent.Should().Contain("initialize", "n8n-mcp-stub", "tools/list");
    }

    private sealed class FakeWatchdog : IMcpLaneWatchdog
    {
        private readonly IReadOnlyList<McpLaneWatchdogSnapshot> _snapshot;

        public FakeWatchdog(IReadOnlyList<McpLaneWatchdogSnapshot> snapshot) => _snapshot = snapshot;

        public void PerformWatchdogCheck() { }

        public IReadOnlyList<McpLaneWatchdogSnapshot> GetSnapshot() => _snapshot;

        public IReadOnlyList<McpLaneWatchdogHistoryEntry> GetHistory(string profileKey) => Array.Empty<McpLaneWatchdogHistoryEntry>();
    }

    private sealed class FakeRegistry : IMcpToolRegistry
    {
        private readonly IReadOnlyList<McpToolMetadata> _tools;

        public FakeRegistry(IReadOnlyList<McpToolMetadata> tools) => _tools = tools;

        public IReadOnlyList<McpToolMetadata> ListTools() => _tools;

        public McpToolMetadata? FindTool(string toolName) =>
            _tools.FirstOrDefault(t => t.ToolName.Equals(toolName, StringComparison.OrdinalIgnoreCase));
    }
}
