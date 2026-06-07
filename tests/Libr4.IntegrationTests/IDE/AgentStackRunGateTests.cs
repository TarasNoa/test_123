using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AgentStackRunGateTests
{
    [Fact]
    public async Task EnsureReadyForRun_WhenDisabled_DoesNotThrow()
    {
        var health = new StubHealth(new AgentStackHealthStatus(
            true, true, true, true, true,
            Array.Empty<AgentStackComponentHealth>()));
        var sut = new AgentStackRunGate(
            health,
            Options.Create(new AgentStackOptions { RequireHealthyBeforeRun = false }),
            NullLogger<AgentStackRunGate>.Instance);

        await sut.Invoking(s => s.EnsureReadyForRunAsync()).Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureReadyForRun_WhenUnhealthy_ThrowsAgentStackUnhealthyException()
    {
        var status = new AgentStackHealthStatus(
            false, true, true, true, true,
            [new AgentStackComponentHealth("obscura", false, "down")]);
        var sut = new AgentStackRunGate(
            new StubHealth(status),
            Options.Create(new AgentStackOptions { RequireHealthyBeforeRun = true }),
            NullLogger<AgentStackRunGate>.Instance);

        await sut.Invoking(s => s.EnsureReadyForRunAsync())
            .Should().ThrowAsync<AgentStackUnhealthyException>();
    }

    private sealed class StubHealth : IAgentStackHealthService
    {
        private readonly AgentStackHealthStatus _status;

        public StubHealth(AgentStackHealthStatus status) => _status = status;

        public Task<AgentStackHealthStatus> CheckAsync(CancellationToken ct = default) =>
            Task.FromResult(_status);
    }
}
