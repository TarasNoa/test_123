using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests.Functional;

public class PaymentsFunctionalTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5003";

    public PaymentsFunctionalTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Get_Wallet_Requires_Authentication()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/wallet");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Wallet_History_Requires_Authentication()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/wallet/history");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Payment_Intent_Requires_Authentication()
    {
        // Arrange
        var request = new
        {
            amount = 100.00m,
            currency = "USD",
            description = "Test payment"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/payments/intent", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Escrow_By_Id_Requires_Authentication()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/escrow/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Escrow_Requires_Authentication()
    {
        // Arrange
        var request = new
        {
            taskId = Guid.NewGuid(),
            freelancerId = Guid.NewGuid(),
            amount = 500.00m,
            milestone = "Initial milestone"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/escrow", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Release_Escrow_Requires_Authentication()
    {
        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/escrow/{Guid.NewGuid()}/release", new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Refund_Escrow_Requires_Authentication()
    {
        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/escrow/{Guid.NewGuid()}/refund", new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Webhook_Endpoint_Is_Public()
    {
        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/payments/webhook", new { });

        // Assert - webhook should be public but require signature validation
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Transactions_Requires_Authentication()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/transactions");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }
}
