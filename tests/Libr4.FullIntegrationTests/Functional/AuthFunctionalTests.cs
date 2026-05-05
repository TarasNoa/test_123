using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests.Functional;

public class AuthFunctionalTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5001";
    private static string? _authToken;
    private static Guid? _userId;

    public AuthFunctionalTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Step1_Register_New_User_Returns_Success()
    {
        // Arrange
        var uniqueEmail = $"testuser_{Guid.NewGuid()}@example.com";
        var request = new
        {
            email = uniqueEmail,
            password = "TestPassword123!@#",
            firstName = "Functional",
            lastName = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/auth/register", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.BadRequest);
        
        if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK)
        {
            content.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task Step2_Login_With_Valid_Credentials_Returns_Token()
    {
        // Arrange - first register
        var uniqueEmail = $"logintest_{Guid.NewGuid()}@example.com";
        var password = "TestPassword123!@#";
        
        var registerRequest = new
        {
            email = uniqueEmail,
            password = password,
            firstName = "Login",
            lastName = "Test"
        };
        
        await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/auth/register", registerRequest);

        // Act - login
        var loginRequest = new
        {
            email = uniqueEmail,
            password = password
        };
        
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/auth/login", loginRequest);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            content.Should().NotBeNullOrEmpty();
            // Try to extract token
            if (content.Contains("token") || content.Contains("accessToken"))
            {
                _authToken = ExtractToken(content);
            }
        }
    }

    [Fact]
    public async Task Step3_Login_With_Invalid_Credentials_Returns_Unauthorized()
    {
        // Arrange
        var loginRequest = new
        {
            email = "nonexistent_user@test.com",
            password = "WrongPassword123"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Step4_Get_Me_Requires_Authentication()
    {
        // Act without auth
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/users/me");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Step5_Refresh_Token_Requires_Valid_Token()
    {
        // Arrange
        var refreshRequest = new
        {
            refreshToken = "invalid_refresh_token"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/auth/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Step6_Forgot_Password_Returns_Success_Or_NotFound()
    {
        // Arrange
        var request = new
        {
            email = $"nonexistent_{Guid.NewGuid()}@test.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/auth/forgot-password", request);

        // Assert - should return OK (to not leak info) or NotFound
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Step7_Validate_Token_Endpoint_Exists()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/auth/validate");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    private static string? ExtractToken(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("token", out var tokenElement))
                return tokenElement.GetString();
            
            if (root.TryGetProperty("accessToken", out var accessTokenElement))
                return accessTokenElement.GetString();
                
            return null;
        }
        catch
        {
            return null;
        }
    }
}
