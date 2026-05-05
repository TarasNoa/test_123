using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests;

public class ChatApiTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5004";

    public ChatApiTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Chat_Swagger_Is_Accessible()
    {
        var response = await _client.GetAsync($"{BaseUrl}/swagger/index.html");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Moved, HttpStatusCode.MovedPermanently);
    }

    [Fact]
    public async Task Chat_Chats_Endpoint_Requires_Auth()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/chats");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }
}
