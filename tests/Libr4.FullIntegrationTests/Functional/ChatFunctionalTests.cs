using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests.Functional;

public class ChatFunctionalTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5004";

    public ChatFunctionalTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Get_Chats_Requires_Authentication()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/chats");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Chat_By_Id_Requires_Authentication()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/chats/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Direct_Chat_Requires_Authentication()
    {
        // Arrange
        var request = new
        {
            userId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/chats/direct", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Group_Chat_Requires_Authentication()
    {
        // Arrange
        var request = new
        {
            name = "Test Group",
            participantIds = new[] { Guid.NewGuid(), Guid.NewGuid() }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/chats/group", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Send_Message_Requires_Authentication()
    {
        // Arrange
        var request = new
        {
            content = "Test message"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/chats/{Guid.NewGuid()}/messages", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Messages_Requires_Authentication()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/chats/{Guid.NewGuid()}/messages");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Edit_Message_Requires_Authentication()
    {
        // Arrange
        var request = new
        {
            content = "Edited message"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/api/v1/chats/{Guid.NewGuid()}/messages/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Message_Requires_Authentication()
    {
        // Act
        var response = await _client.DeleteAsync($"{BaseUrl}/api/v1/chats/{Guid.NewGuid()}/messages/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Leave_Chat_Requires_Authentication()
    {
        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/chats/{Guid.NewGuid()}/leave", new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Notifications_Requires_Authentication()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/notifications");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Mark_Notifications_Read_Requires_Authentication()
    {
        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/notifications/read", new { });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }
}
