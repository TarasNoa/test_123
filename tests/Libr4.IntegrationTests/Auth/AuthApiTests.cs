using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Libr4.IntegrationTests.Fixtures;
using Xunit;

namespace Libr4.IntegrationTests.Auth;

[Collection("IntegrationTests")]
public class AuthApiTests
{
    private readonly IntegrationTestFixture _fixture;
    private readonly HttpClient _client;

    public AuthApiTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        // Create WebApplicationFactory with test configuration
        var factory = new CustomWebApplicationFactory(fixture);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_NewUser_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            Email = $"test_{Guid.NewGuid()}@example.com",
            Password = "Test123!@#",
            DisplayName = "Test User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        var content = await response.Content.ReadFromJsonAsync<AuthResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        content.Should().NotBeNull();
        content!.UserId.Should().NotBeEmpty();
        content.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        // Arrange - First register a user
        var registerRequest = new
        {
            Email = $"login_{Guid.NewGuid()}@example.com",
            Password = "Test123!@#",
            DisplayName = "Login Test User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginRequest = new
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var content = await response.Content.ReadFromJsonAsync<AuthResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.AccessToken.Should().NotBeNullOrEmpty();
        content.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private class AuthResponse
    {
        public Guid UserId { get; set; }
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}
