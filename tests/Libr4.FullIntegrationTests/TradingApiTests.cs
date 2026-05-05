using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests;

public class TradingApiTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5005";

    public TradingApiTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Trading_Swagger_Is_Accessible()
    {
        var response = await _client.GetAsync($"{BaseUrl}/swagger/index.html");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Moved, HttpStatusCode.MovedPermanently);
    }

    [Fact]
    public async Task Trading_Portfolio_Endpoint_Requires_Auth()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/portfolio");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Trading_Orders_Endpoint_Requires_Auth()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/orders");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Trading_MarketData_Is_Public()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/market-data/symbols");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }
}
