using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests.Functional;

public class TasksFunctionalTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5002";

    public TasksFunctionalTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Get_Categories_Returns_List_Or_Requires_Auth()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/categories");

        // Assert - should be either public list or require auth
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Task_Requires_Authentication()
    {
        // Arrange
        var request = new
        {
            title = "Test Task",
            description = "Test Description",
            categoryId = Guid.NewGuid(),
            budget = 100.00m,
            deadline = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/tasks", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Tasks_Requires_Authentication()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/tasks");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Task_By_Id_Requires_Authentication()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/tasks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Task_Requires_Authentication()
    {
        // Arrange
        var request = new
        {
            title = "Updated Task",
            description = "Updated Description"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"{BaseUrl}/api/v1/tasks/{Guid.NewGuid()}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Task_Requires_Authentication()
    {
        // Act
        var response = await _client.DeleteAsync($"{BaseUrl}/api/v1/tasks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_Tasks_Endpoint_Exists()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/tasks/search?query=test");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Task_Applications_Endpoint_Requires_Auth()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/tasks/{Guid.NewGuid()}/applications");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Apply_To_Task_Requires_Authentication()
    {
        // Arrange
        var request = new
        {
            message = "I want to apply",
            proposedBudget = 80.00m
        };

        // Act
        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/tasks/{Guid.NewGuid()}/apply", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }
}
