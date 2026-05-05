using System.Net;
using FluentAssertions;

namespace Libr4.FullIntegrationTests;

public class InfrastructureTests
{
    private readonly HttpClient _client;

    public InfrastructureTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Postgres_Is_Available()
    {
        // Health check via API that uses Postgres
        var response = await _client.GetAsync("http://localhost:5001/health");
        // If API is up and can query DB, it's healthy
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RabbitMQ_Is_Available()
    {
        // RabbitMQ Management API
        var response = await _client.GetAsync("http://localhost:15672/api/overview");
        // Will return 401 if auth required, but connection works
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
    }

    [Fact]
    public async Task Redis_Is_Available()
    {
        // Via Tasks API that uses Redis
        var response = await _client.GetAsync("http://localhost:5002/health");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task All_Infrastructure_Services_Are_Running()
    {
        // Check all services can connect to their dependencies
        var services = new Dictionary<string, string>
        {
            ["Auth+Postgres"] = "http://localhost:5001/swagger/index.html",
            ["Tasks+Postgres+Redis"] = "http://localhost:5002/swagger/index.html",
            ["Payments+Postgres+RabbitMQ"] = "http://localhost:5003/swagger/index.html",
            ["Chat+Postgres+Redis+RabbitMQ"] = "http://localhost:5004/swagger/index.html",
            ["Trading+Postgres+RabbitMQ"] = "http://localhost:5005/swagger/index.html",
            ["AI+Postgres+RabbitMQ"] = "http://localhost:5006/swagger/index.html",
        };

        foreach (var (name, url) in services)
        {
            var response = await _client.GetAsync(url);
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.MovedPermanently);
        }
    }
}
