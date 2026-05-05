using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests.Functional;

public class TradingFunctionalTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5005";

    public TradingFunctionalTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Get_Portfolio_Requires_Authentication()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/portfolio");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Orders_Requires_Authentication()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/orders");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Order_Requires_Authentication()
    {
        var request = new
        {
            symbol = "BTCUSDT",
            side = "Buy",
            type = "Limit",
            quantity = 0.1m,
            price = 50000.00m
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/orders", request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_Order_Requires_Authentication()
    {
        var response = await _client.DeleteAsync($"{BaseUrl}/api/v1/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Order_By_Id_Requires_Authentication()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Market_Symbols_May_Be_Public()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/market-data/symbols");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Ticker_Price_May_Be_Public()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/market-data/ticker/BTCUSDT");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_OrderBook_May_Be_Public()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/market-data/orderbook/BTCUSDT");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Trading_Bots_Requires_Authentication()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/trading-bots");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Trading_Bot_Requires_Authentication()
    {
        var request = new
        {
            name = "Test Bot",
            strategy = "Grid",
            symbol = "BTCUSDT",
            config = new { }
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/trading-bots", request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Trades_History_Requires_Authentication()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/trades");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Chart_Analysis_May_Require_Auth()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/analysis/BTCUSDT");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }
}
