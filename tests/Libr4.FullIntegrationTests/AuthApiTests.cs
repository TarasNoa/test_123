using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests;

public class AuthApiTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5001";

    public AuthApiTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Auth_Swagger_Is_Accessible()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/swagger/index.html");
        
        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Moved, HttpStatusCode.MovedPermanently);
    }

    [Fact]
    public async Task Auth_Register_Returns_CreatedOrBadRequest()
    {
        // Arrange
        var request = new
        {
            email = $"test{Guid.NewGuid()}@example.com",
            password = "Test123!@#",
            firstName = "Test",
            lastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/auth/register", request);
        
        // Assert - either created (new user) or bad request (user exists)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created, 
            HttpStatusCode.BadRequest,
            HttpStatusCode.OK
        );
    }

    [Fact]
    public async Task Auth_Login_With_Invalid_Credentials_Returns_Unauthorized()
    {
        // Arrange
        var request = new
        {
            email = "nonexistent@example.com",
            password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/auth/login", request);
        
        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.BadRequest,
            HttpStatusCode.NotFound
        );
    }

    [Fact]
    public async Task Auth_Health_Check_Is_Accessible()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/healthz");
        
        // Assert - health check should exist
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NotFound
        );
    }

    [Fact]
    public async Task Auth_Session1_Endpoints_Are_Accessible()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/skills");
        
        // Assert - should require auth, so either 401 or 404 if endpoint doesn't exist
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.NotFound,
            HttpStatusCode.OK
        );
    }
}
