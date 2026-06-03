using System.Net;
using FluentAssertions;

namespace Libr4.FullIntegrationTests;

public class ObscuraServicesIntegrationTests
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

    [Fact]
    public async Task ShadowSync_Health_ShouldBeHealthy()
    {
        var response = await Client.GetAsync("http://localhost:8080/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SecurityScanner_Health_ShouldBeHealthy()
    {
        var response = await Client.GetAsync("http://localhost:7070/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BinaryArcheology_Health_ShouldBeHealthy()
    {
        var response = await Client.GetAsync("http://localhost:6060/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SandboxController_Health_ShouldBeHealthy()
    {
        var response = await Client.GetAsync("http://localhost:9090/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
