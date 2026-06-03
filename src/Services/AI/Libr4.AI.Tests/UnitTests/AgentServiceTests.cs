using Xunit;
using Moq;
using Libr4.AI.Application.Agents;
using Libr4.AI.Domain.Agents;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Tests.UnitTests;

public class AgentServiceTests
{
    private readonly Mock<IAgentRepository> _agentRepositoryMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<AgentService>> _loggerMock;
    private readonly AgentService _service;

    public AgentServiceTests()
    {
        _agentRepositoryMock = new Mock<IAgentRepository>();
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<AgentService>>();
        _service = new AgentService(_agentRepositoryMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAgentsAsync_ReturnsAgentsFromRepository()
    {
        // Arrange
        var agents = new List<Agent>
        {
            Agent.Create("TestAgent", "TestRole", "TestPrompt")
        };
        _agentRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(agents);
        _cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _service.GetAgentsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("TestAgent", result[0].Name);
    }

    // More tests...
}