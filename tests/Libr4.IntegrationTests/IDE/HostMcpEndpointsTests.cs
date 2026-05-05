using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Libr4.IDE.AutonomousAppGeneration.Host;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class HostMcpEndpointsTests
    : IClassFixture<WebApplicationFactory<AutonomousAppGenerationHostWebApplicationFactoryAnchor>>
{
    private readonly WebApplicationFactory<AutonomousAppGenerationHostWebApplicationFactoryAnchor> _factory;

    public HostMcpEndpointsTests(WebApplicationFactory<AutonomousAppGenerationHostWebApplicationFactoryAnchor> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task McpTools_ShouldReturnOkJsonArray()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync(new Uri("/api/ide/app-generation/mcp/tools", UriKind.Relative));
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var doc = await res.Content.ReadFromJsonAsync<JsonElement>();
        doc.ValueKind.Should().Be(JsonValueKind.Array);
    }
}
