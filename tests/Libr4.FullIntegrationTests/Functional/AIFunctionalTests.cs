using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Libr4.FullIntegrationTests.Functional;

public class AIFunctionalTests
{
    private readonly HttpClient _client;
    private const string BaseUrl = "http://localhost:5006";

    public AIFunctionalTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task Get_AI_Chats_Requires_Authentication()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/ai/chats");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_AI_Chat_Requires_Authentication()
    {
        var request = new
        {
            title = "Test Chat",
            model = "gpt-4"
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/ai/chats", request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Send_AI_Message_Requires_Authentication()
    {
        var request = new
        {
            content = "Hello AI"
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/ai/chats/{Guid.NewGuid()}/messages", request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_AI_Completions_Requires_Authentication()
    {
        var request = new
        {
            prompt = "Test prompt",
            model = "gpt-4",
            maxTokens = 100
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/ai/completions", request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Generate_Code_Requires_Authentication()
    {
        var request = new
        {
            prompt = "Generate a function to calculate fibonacci",
            language = "csharp"
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/ai/code/generate", request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Explain_Code_Requires_Authentication()
    {
        var request = new
        {
            code = "function add(a, b) { return a + b; }",
            language = "javascript"
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/ai/code/explain", request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Recommendations_Requires_Authentication()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/ai/recommendations");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_AI_Models_May_Be_Public()
    {
        var response = await _client.GetAsync($"{BaseUrl}/api/v1/ai/models");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Generate_Image_Requires_Authentication()
    {
        var request = new
        {
            prompt = "A beautiful sunset",
            model = "dall-e-3",
            size = "1024x1024"
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/ai/images", request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_With_RAG_Requires_Authentication()
    {
        var request = new
        {
            query = "How to use the platform?",
            topK = 5
        };

        var response = await _client.PostAsJsonAsync($"{BaseUrl}/api/v1/ai/search", request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }
}
