using System.Net;
using System.Net.Http.Json;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Libr4.AI.Tests.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Libr4.AI.Api.Program>>
{
    private readonly WebApplicationFactory<Libr4.AI.Api.Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Libr4.AI.Api.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAgents_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/ai/agents");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // More integration tests...
}