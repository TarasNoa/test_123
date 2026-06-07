using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Fleet;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AgentFleetEventHubTests
{
    [Fact]
    public async Task PublishAsync_NotifiesSubscribers()
    {
        var hub = new AgentFleetEventHub();
        var runId = Guid.NewGuid();
        AgentFleetStatusEvent? received = null;

        hub.EventPublished += evt =>
        {
            received = evt;
            return Task.CompletedTask;
        };

        var payload = new AgentFleetStatusEvent(runId, AgentFleetStatus.Verifying, "verify-stage", DateTime.UtcNow);
        await hub.PublishAsync(payload);

        received.Should().NotBeNull();
        received!.RunId.Should().Be(runId);
        received.Status.Should().Be(AgentFleetStatus.Verifying);
        received.Stage.Should().Be("verify-stage");
    }
}
