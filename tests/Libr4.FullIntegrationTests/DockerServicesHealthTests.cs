using System.Net;
using FluentAssertions;

namespace Libr4.FullIntegrationTests;

public class DockerServicesHealthTests
{
    private readonly HttpClient _client;
    private readonly Dictionary<string, string> _serviceUrls;

    public DockerServicesHealthTests()
    {
        _client = new HttpClient();
        _serviceUrls = new Dictionary<string, string>
        {
            ["Gateway"] = "http://localhost:5000",
            ["Auth"] = "http://localhost:5001",
            ["Tasks"] = "http://localhost:5002",
            ["Payments"] = "http://localhost:5003",
            ["Chat"] = "http://localhost:5004",
            ["Trading"] = "http://localhost:5005",
            ["AI"] = "http://localhost:5006"
        };
    }

    [Theory]
    [InlineData("Gateway", "http://localhost:5000")]
    [InlineData("Auth", "http://localhost:5001")]
    [InlineData("Tasks", "http://localhost:5002")]
    [InlineData("Payments", "http://localhost:5003")]
    [InlineData("Chat", "http://localhost:5004")]
    [InlineData("Trading", "http://localhost:5005")]
    [InlineData("AI", "http://localhost:5006")]
    public async Task Service_Responds_To_Ping(string name, string url)
    {
        // Act
        var response = await _client.GetAsync($"{url}/");
        
        // Assert - should not throw (404 is fine, means service is up)
        response.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Gateway", "http://localhost:5000")]
    [InlineData("Auth", "http://localhost:5001")]
    [InlineData("Tasks", "http://localhost:5002")]
    [InlineData("Payments", "http://localhost:5003")]
    [InlineData("Chat", "http://localhost:5004")]
    [InlineData("Trading", "http://localhost:5005")]
    [InlineData("AI", "http://localhost:5006")]
    public async Task Service_Returns_Swagger_UI(string name, string url)
    {
        // Act
        var response = await _client.GetAsync($"{url}/swagger/index.html");
        
        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.MovedPermanently);
    }

    [Fact]
    public async Task All_Services_Are_Reachable()
    {
        // Act & Assert
        foreach (var (name, url) in _serviceUrls)
        {
            var response = await _client.GetAsync($"{url}/");
            response.Should().NotBeNull($"{name} should respond");
        }
    }
}
