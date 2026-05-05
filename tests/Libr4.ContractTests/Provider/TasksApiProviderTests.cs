using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Output.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Libr4.ContractTests.Provider;

public class TasksApiProviderTests : IClassFixture<ProviderStateMiddleware>
{
    private readonly ITestOutputHelper _output;
    private readonly ProviderStateMiddleware _fixture;

    public TasksApiProviderTests(ITestOutputHelper output, ProviderStateMiddleware fixture)
    {
        _output = output;
        _fixture = fixture;
    }

    [Fact]
    public void VerifyTasksApiHonoursPactWithFrontend()
    {
        // Arrange
        var config = new PactVerifierConfig
        {
            Outputters = new List<IOutput> { new XunitOutput(_output) },
            Verbose = true
        };

        var pactPath = Path.Combine("..", "..", "..", "pacts", "Libr4-Frontend-Libr4-Tasks-API.json");

        // Act & Assert
        IPactVerifier pactVerifier = new PactVerifier(config);
        
        pactVerifier
            .ServiceProvider("Libr4-Tasks-API", _fixture.ServerUri)
            .PactBroker(
                Environment.GetEnvironmentVariable("PACT_BROKER_BASE_URL") ?? "http://localhost:9292",
                new PactBrokerOptions
                {
                    Token = Environment.GetEnvironmentVariable("PACT_BROKER_TOKEN"),
                    ConsumerVersionSelectors = new List<VersionSelector>
                    {
                        new() { MainBranch = true },
                        new() { DeployedOrReleased = true }
                    }
                })
            .WithProviderStateUrl(new Uri($"{_fixture.ServerUri}/provider-states"))
            .Verify();
    }
}

public class ProviderStateMiddleware : IDisposable
{
    public string ServerUri { get; }
    private readonly TestServer _server;

    public ProviderStateMiddleware()
    {
        ServerUri = "http://localhost:9222";
        
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                // Configure test services
                services.AddRouting();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapPost("/provider-states", async context =>
                    {
                        var state = await context.Request.ReadFromJsonAsync<ProviderState>();
                        
                        // Set up provider state based on the consumer's request
                        switch (state?.State)
                        {
                            case "tasks exist":
                                // Seed test data
                                break;
                            case "no tasks exist":
                                // Clear test data
                                break;
                        }
                        
                        context.Response.StatusCode = 200;
                    });
                });
            });

        _server = new TestServer(builder);
    }

    public void Dispose()
    {
        _server.Dispose();
    }
}

public class ProviderState
{
    public string State { get; set; } = "";
    public Dictionary<string, object> Params { get; set; } = new();
}
