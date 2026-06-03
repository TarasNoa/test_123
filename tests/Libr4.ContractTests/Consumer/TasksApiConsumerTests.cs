using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PactNet;
using PactNet.Matchers;
using PactNet.Output.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Libr4.ContractTests.Consumer;

public class TasksApiConsumerTests
{
    private readonly IPactBuilderV4 _pact;
    private readonly ITestOutputHelper _output;

    public TasksApiConsumerTests(ITestOutputHelper output)
    {
        _output = output;

        var config = new PactConfig
        {
            PactDir = "../../../pacts/",
            DefaultJsonSettings = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            },
            Outputters = new[] { new XunitOutput(output) }
        };

        _pact = Pact.V4("Libr4-Frontend", "Libr4-Tasks-API", config)
            .WithHttpInteractions();
    }

    [Fact]
    public async Task GetTasks_ReturnsPagedList()
    {
        var expectedResponse = new
        {
            items = new[]
            {
                new
                {
                    id = Match.Type(Guid.NewGuid()),
                    title = Match.Type("Test Task"),
                    description = Match.Type("Description"),
                    status = Match.Regex("Open", "^(Open|InProgress|Completed|Cancelled)$"),
                    budget = Match.Decimal(100.00m),
                    createdAt = Match.Type("2024-01-01T00:00:00Z")
                }
            },
            totalCount = Match.Integer(1),
            page = Match.Integer(1),
            pageSize = Match.Integer(20)
        };

        _pact
            .UponReceiving("a request for tasks list")
            .WithRequest(HttpMethod.Get, "/api/v1/tasks")
            .WithQuery("page", "1")
            .WithQuery("pageSize", "20")
            .WithHeader("Authorization", Match.Regex("Bearer .*", "^Bearer .+"))
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(expectedResponse);

        await _pact.VerifyAsync(async ctx =>
        {
            using var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "test-token");

            var response = await client.GetAsync("/api/v1/tasks?page=1&pageSize=20");
            var content = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().Contain("items");
        });
    }

    [Fact]
    public async Task CreateTask_ReturnsCreatedTask()
    {
        var requestBody = new
        {
            title = "New Task",
            description = "Task description",
            categoryId = Guid.NewGuid(),
            budget = 150.00m,
            deadline = DateTime.UtcNow.AddDays(7)
        };

        var expectedResponse = new
        {
            id = Match.Type(Guid.NewGuid()),
            title = Match.Type("New Task"),
            description = Match.Type("Task description"),
            status = Match.Regex("Draft", "^Draft$"),
            budget = Match.Decimal(150.00m),
            createdAt = Match.Type("2024-01-01T00:00:00Z")
        };

        _pact
            .UponReceiving("a request to create a task")
            .WithRequest(HttpMethod.Post, "/api/v1/tasks")
            .WithHeader("Content-Type", "application/json")
            .WithHeader("Authorization", Match.Regex("Bearer .*", "^Bearer .+"))
            .WithJsonBody(requestBody)
            .WillRespond()
            .WithStatus(HttpStatusCode.Created)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(expectedResponse);

        await _pact.VerifyAsync(async ctx =>
        {
            using var client = new HttpClient { BaseAddress = ctx.MockServerUri };
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "test-token");

            var response = await client.PostAsJsonAsync("/api/v1/tasks", requestBody);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        });
    }
}
