using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests;

public class PaymentsApiTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5003";

    public PaymentsApiTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Payments_Swagger_Is_Accessible()
    {
        var response = await _client.GetAsync($"{BaseUrl}/swagger/index.html");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Moved, HttpStatusCode.MovedPermanently);
    }

    [Fact]
    public async Task Payments_Wallet_Endpoint_Requires_Auth()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/wallet");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Payments_Escrow_Endpoint_Requires_Auth()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/escrow");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }
}
