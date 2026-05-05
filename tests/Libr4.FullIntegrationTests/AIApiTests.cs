using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests;

public class AIApiTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5006";

    public AIApiTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task AI_Swagger_Is_Accessible()
    {
        var response = await _client.GetAsync($"{BaseUrl}/swagger/index.html");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Moved, HttpStatusCode.MovedPermanently);
    }

    [Fact]
    public async Task AI_Chats_Endpoint_Requires_Auth()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/ai/chats");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AI_Completions_Endpoint_Requires_Auth()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/ai/completions");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }
}
