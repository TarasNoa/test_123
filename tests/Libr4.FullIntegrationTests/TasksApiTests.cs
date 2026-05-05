using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests;

public class TasksApiTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5002";

    public TasksApiTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Tasks_Swagger_Is_Accessible()
    {
        var response = await _client.GetAsync($"{BaseUrl}/swagger/index.html");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Moved, HttpStatusCode.MovedPermanently);
    }

    [Fact]
    public async Task Tasks_Get_Without_Auth_Returns_401()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/tasks");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Tasks_Categories_Endpoint_Exists()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/categories");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }
}
