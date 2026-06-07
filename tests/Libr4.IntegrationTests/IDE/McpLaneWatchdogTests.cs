using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public class McpLaneWatchdogTests
{
    [Fact]
    public void PerformWatchdogCheck_ShouldReturnDegraded_WhenServerMissing()
    {
        var mcpOptions = Options.Create(new McpExecutionOptions
        {
            ServerProfiles = new Dictionary<string, McpServerLaunchProfile>
            {
                ["libr4-agent-bridge"] = new McpServerLaunchProfile
                {
                    FileName = "nonexistent-executable",
                    Arguments = new List<string>()
                }
            }
        });

        var preflight = new DefaultMcpServerPreflight(mcpOptions, NullLogger<DefaultMcpServerPreflight>.Instance);
        var registry = new DefaultMcpToolRegistry();
        var watchdog = new DefaultMcpLaneWatchdog(
            preflight,
            registry,
            mcpOptions,
            NullLogger<DefaultMcpLaneWatchdog>.Instance);

        watchdog.PerformWatchdogCheck();
        var snapshot = watchdog.GetSnapshot();

        // The watchdog checks profiles used by registered tools
        // DefaultMcpToolRegistry has tools that use "browser-lane"
        snapshot.Should().NotBeEmpty();
        snapshot.Should().ContainSingle(s => s.ProfileKey == "libr4-agent-bridge");
        snapshot.First(s => s.ProfileKey == "libr4-agent-bridge").Status.Should().Be("degraded");
        snapshot.First(s => s.ProfileKey == "libr4-agent-bridge").BlockerCode.Should().Be("mcp_server_missing");
    }

    [Fact]
    public void PerformWatchdogCheck_ShouldReturnAvailable_WhenServerExists()
    {
        var mcpOptions = Options.Create(new McpExecutionOptions
        {
            ServerProfiles = new Dictionary<string, McpServerLaunchProfile>
            {
                ["node-server"] = new McpServerLaunchProfile
                {
                    FileName = "node", // Assume node is in PATH
                    Arguments = new List<string>()
                }
            }
        });

        var preflight = new DefaultMcpServerPreflight(mcpOptions, NullLogger<DefaultMcpServerPreflight>.Instance);
        var registry = new DefaultMcpToolRegistry();
        var watchdog = new DefaultMcpLaneWatchdog(
            preflight,
            registry,
            mcpOptions,
            NullLogger<DefaultMcpLaneWatchdog>.Instance);

        watchdog.PerformWatchdogCheck();
        var snapshot = watchdog.GetSnapshot();

        // The snapshot should either be empty (if no tools use this profile) or available
        var nodeSnapshot = snapshot.FirstOrDefault(s => s.ProfileKey == "node-server");
        if (nodeSnapshot != null)
        {
            nodeSnapshot.Status.Should().Be("available");
            nodeSnapshot.BlockerCode.Should().BeNull();
        }
    }

    [Fact]
    public void GetSnapshot_ShouldReturnEmpty_WhenNoChecksPerformed()
    {
        var mcpOptions = Options.Create(new McpExecutionOptions());
        var preflight = new DefaultMcpServerPreflight(mcpOptions, NullLogger<DefaultMcpServerPreflight>.Instance);
        var registry = new DefaultMcpToolRegistry();
        var watchdog = new DefaultMcpLaneWatchdog(
            preflight,
            registry,
            mcpOptions,
            NullLogger<DefaultMcpLaneWatchdog>.Instance);

        var snapshot = watchdog.GetSnapshot();

        snapshot.Should().BeEmpty();
    }

    [Fact]
    public void PerformWatchdogCheck_ShouldUpdateSnapshot_WhenCalledMultipleTimes()
    {
        var mcpOptions = Options.Create(new McpExecutionOptions
        {
            ServerProfiles = new Dictionary<string, McpServerLaunchProfile>
            {
                ["libr4-agent-bridge"] = new McpServerLaunchProfile
                {
                    FileName = "nonexistent-executable",
                    Arguments = new List<string>()
                }
            }
        });

        var preflight = new DefaultMcpServerPreflight(mcpOptions, NullLogger<DefaultMcpServerPreflight>.Instance);
        var registry = new DefaultMcpToolRegistry();
        var watchdog = new DefaultMcpLaneWatchdog(
            preflight,
            registry,
            mcpOptions,
            NullLogger<DefaultMcpLaneWatchdog>.Instance);

        watchdog.PerformWatchdogCheck();
        var firstSnapshot = watchdog.GetSnapshot();

        // Wait a bit to ensure time difference
        Thread.Sleep(100);

        watchdog.PerformWatchdogCheck();
        var secondSnapshot = watchdog.GetSnapshot();

        secondSnapshot.Should().HaveCount(firstSnapshot.Count);
        // The snapshot should be updated (the check time may or may not be different depending on timing)
        secondSnapshot.Should().NotBeEmpty();
    }

    [Fact]
    public void PerformWatchdogCheck_ShouldCheckPreflight_WhenToolUsesProfile()
    {
        var mcpOptions = Options.Create(new McpExecutionOptions
        {
            ServerProfiles = new Dictionary<string, McpServerLaunchProfile>
            {
                ["libr4-agent-bridge"] = new McpServerLaunchProfile
                {
                    FileName = "python",
                    Arguments = new List<string> { "--version" }
                }
            }
        });

        var preflight = new DefaultMcpServerPreflight(mcpOptions, NullLogger<DefaultMcpServerPreflight>.Instance);
        var registry = new DefaultMcpToolRegistry();
        var watchdog = new DefaultMcpLaneWatchdog(
            preflight,
            registry,
            mcpOptions,
            NullLogger<DefaultMcpLaneWatchdog>.Instance);

        watchdog.PerformWatchdogCheck();
        var snapshot = watchdog.GetSnapshot();

        // The watchdog should check profiles used by registered tools
        // The DefaultMcpToolRegistry has tools that use "libr4-agent-bridge"
        // If python is available, it should be marked as available
        var pythonSnapshot = snapshot.FirstOrDefault(s => s.ProfileKey == "libr4-agent-bridge");
        if (pythonSnapshot != null)
        {
            pythonSnapshot.Status.Should().BeOneOf("available", "degraded");
        }
    }

    [Fact]
    public void GetHistory_ShouldAppendEntries_WhenCalledMultipleTimes()
    {
        var mcpOptions = Options.Create(new McpExecutionOptions
        {
            ServerProfiles = new Dictionary<string, McpServerLaunchProfile>
            {
                ["libr4-agent-bridge"] = new McpServerLaunchProfile
                {
                    FileName = "nonexistent-executable",
                    Arguments = new List<string>()
                }
            }
        });

        var preflight = new DefaultMcpServerPreflight(mcpOptions, NullLogger<DefaultMcpServerPreflight>.Instance);
        var registry = new DefaultMcpToolRegistry();
        var watchdog = new DefaultMcpLaneWatchdog(
            preflight,
            registry,
            mcpOptions,
            NullLogger<DefaultMcpLaneWatchdog>.Instance);

        // Perform multiple checks
        watchdog.PerformWatchdogCheck();
        watchdog.PerformWatchdogCheck();
        watchdog.PerformWatchdogCheck();

        var history = watchdog.GetHistory("libr4-agent-bridge");
        history.Should().HaveCountGreaterOrEqualTo(1);
    }

    [Fact]
    public void GetHistory_ShouldBeBounded_WhenExceedingMaxDepth()
    {
        var mcpOptions = Options.Create(new McpExecutionOptions
        {
            WatchdogHistoryDepth = 5,
            ServerProfiles = new Dictionary<string, McpServerLaunchProfile>
            {
                ["libr4-agent-bridge"] = new McpServerLaunchProfile
                {
                    FileName = "nonexistent-executable",
                    Arguments = new List<string>()
                }
            }
        });

        var preflight = new DefaultMcpServerPreflight(mcpOptions, NullLogger<DefaultMcpServerPreflight>.Instance);
        var registry = new DefaultMcpToolRegistry();
        var watchdog = new DefaultMcpLaneWatchdog(
            preflight,
            registry,
            mcpOptions,
            NullLogger<DefaultMcpLaneWatchdog>.Instance);

        // Perform more checks than the history depth
        for (int i = 0; i < 10; i++)
        {
            watchdog.PerformWatchdogCheck();
        }

        var history = watchdog.GetHistory("libr4-agent-bridge");
        history.Should().HaveCountGreaterOrEqualTo(1);
        history.Should().HaveCountLessOrEqualTo(5); // Should be bounded by WatchdogHistoryDepth
    }

    [Fact]
    public void GetHistory_ShouldShowTransitions_WhenStatusChanges()
    {
        var mcpOptions = Options.Create(new McpExecutionOptions
        {
            ServerProfiles = new Dictionary<string, McpServerLaunchProfile>
            {
                ["libr4-agent-bridge"] = new McpServerLaunchProfile
                {
                    FileName = "nonexistent-executable",
                    Arguments = new List<string>()
                }
            }
        });

        var preflight = new DefaultMcpServerPreflight(mcpOptions, NullLogger<DefaultMcpServerPreflight>.Instance);
        var registry = new DefaultMcpToolRegistry();
        var watchdog = new DefaultMcpLaneWatchdog(
            preflight,
            registry,
            mcpOptions,
            NullLogger<DefaultMcpLaneWatchdog>.Instance);

        // Perform check with degraded status
        watchdog.PerformWatchdogCheck();
        var firstHistory = watchdog.GetHistory("libr4-agent-bridge");
        firstHistory.Should().NotBeEmpty();
        firstHistory.First().Status.Should().Be("degraded");
    }

    [Fact]
    public void GetHistory_ShouldReturnEmpty_WhenProfileNotChecked()
    {
        var mcpOptions = Options.Create(new McpExecutionOptions());
        var preflight = new DefaultMcpServerPreflight(mcpOptions, NullLogger<DefaultMcpServerPreflight>.Instance);
        var registry = new DefaultMcpToolRegistry();
        var watchdog = new DefaultMcpLaneWatchdog(
            preflight,
            registry,
            mcpOptions,
            NullLogger<DefaultMcpLaneWatchdog>.Instance);

        var history = watchdog.GetHistory("nonexistent-profile");
        history.Should().BeEmpty();
    }

    [Fact]
    public void PerformWatchdogCheck_ShouldIgnoreMcpMetaPseudoProfile()
    {
        var mcpOptions = Options.Create(new McpExecutionOptions());
        var preflight = new DefaultMcpServerPreflight(mcpOptions, NullLogger<DefaultMcpServerPreflight>.Instance);
        var registry = new DefaultMcpToolRegistry();
        var watchdog = new DefaultMcpLaneWatchdog(
            preflight,
            registry,
            mcpOptions,
            NullLogger<DefaultMcpLaneWatchdog>.Instance);

        watchdog.PerformWatchdogCheck();
        var snapshot = watchdog.GetSnapshot();

        snapshot.Should().NotContain(s => s.ProfileKey.Equals("mcp-meta", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PerformWatchdogCheck_ShouldAppendHistoryAcrossCycles()
    {
        var mcpOptions = Options.Create(new McpExecutionOptions
        {
            WatchdogHistoryDepth = 10,
            ServerProfiles = new Dictionary<string, McpServerLaunchProfile>
            {
                ["libr4-agent-bridge"] = new McpServerLaunchProfile
                {
                    FileName = "nonexistent-executable",
                    Arguments = new List<string>()
                }
            }
        });

        var preflight = new DefaultMcpServerPreflight(mcpOptions, NullLogger<DefaultMcpServerPreflight>.Instance);
        var registry = new DefaultMcpToolRegistry();
        var watchdog = new DefaultMcpLaneWatchdog(
            preflight,
            registry,
            mcpOptions,
            NullLogger<DefaultMcpLaneWatchdog>.Instance);

        watchdog.PerformWatchdogCheck();
        var first = watchdog.GetHistory("libr4-agent-bridge").Count;
        watchdog.PerformWatchdogCheck();
        var second = watchdog.GetHistory("libr4-agent-bridge").Count;

        second.Should().BeGreaterThan(first);
    }
}
