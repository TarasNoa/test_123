using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Libr4.IntegrationTests.Fixtures;
using Xunit;

namespace Libr4.IntegrationTests.Auth;

[Collection("IntegrationTests")]
public class AuthApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
            DisplayName = "Test User",
            Role = "freelancer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var content = JsonSerializer.Deserialize<AuthTokensResponse>(body, JsonOptions);

        // Assert — register auto-logs in and returns tokens (200), not 201
        content.Should().NotBeNull();
        content!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        // Arrange - First register a user
        var registerRequest = new
        {
            Email = $"login_{Guid.NewGuid()}@example.com",
            Password = "Test123!@#",
            DisplayName = "Login Test User",
            Role = "freelancer"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginRequest = new
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var content = JsonSerializer.Deserialize<AuthTokensResponse>(body, JsonOptions);

        // Assert
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

    private class AuthTokensResponse
    {
        public string AccessToken { get; set; } = "";
        public DateTimeOffset AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = "";
        public DateTimeOffset RefreshTokenExpiresAt { get; set; }
    }
}
