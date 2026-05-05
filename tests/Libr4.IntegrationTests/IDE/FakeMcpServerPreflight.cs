using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

namespace Libr4.IntegrationTests.IDE;

/// <summary>
/// Fake preflight for testing - can be configured to always return available or unavailable.
/// </summary>
public sealed class FakeMcpServerPreflight : IMcpServerPreflight
{
    private readonly bool _alwaysAvailable;

    public FakeMcpServerPreflight(bool alwaysAvailable = true)
    {
        _alwaysAvailable = alwaysAvailable;
    }

    public McpServerPreflightResult CheckServerAvailability(string profileKey)
    {
        _ = profileKey;
        return _alwaysAvailable
            ? McpServerPreflightResult.Available()
            : McpServerPreflightResult.ServerMissing($"fake-server-{profileKey}");
    }
}
