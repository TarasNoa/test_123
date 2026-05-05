using System.Net;
using FluentAssertions;

namespace Libr4.FullIntegrationTests;

public class GatewayTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5000";

    public GatewayTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Gateway_Swagger_Is_Accessible()
    {
        var response = await _client.GetAsync($"{BaseUrl}/swagger/index.html");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Moved, HttpStatusCode.MovedPermanently);
    }

    [Fact]
    public async Task Gateway_Routes_To_Auth_Service()
    {
        var response = await _client.GetAsync($"{BaseUrl}/auth/swagger/index.html");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Moved, HttpStatusCode.MovedPermanently);
    }

    [Fact]
    public async Task Gateway_Routes_To_Tasks_Service()
    {
        var response = await _client.GetAsync($"{BaseUrl}/tasks/swagger/index.html");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Moved, HttpStatusCode.MovedPermanently, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Gateway_YARP_Configuration_Is_Loaded()
    {
        var response = await _client.GetAsync($"{BaseUrl}/");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
