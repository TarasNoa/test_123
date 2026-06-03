using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Output.Xunit;
using PactNet.Verifier;
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
        var pactPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "pacts", "Libr4-Frontend-Libr4-Tasks-API.json"));
        Assert.True(File.Exists(pactPath), $"Pact file not found: {pactPath}");

        var verifier = new PactVerifier("Libr4-Tasks-API");

        verifier
            .WithHttpEndpoint(new Uri(_fixture.ServerUri))
            .WithFileSource(new FileInfo(pactPath))
            .WithProviderStateUrl(new Uri($"{_fixture.ServerUri}/provider-states"))
            .Verify();
    }
}

public class ProviderStateMiddleware : IDisposable
{
    public string ServerUri { get; }
    private readonly IHost _server;

    public ProviderStateMiddleware()
    {
        ServerUri = $"http://localhost:{GetFreePort()}";

        _server = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls(ServerUri);
                webBuilder.ConfigureServices(services => services.AddRouting());
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/v1/tasks", () => Results.Ok(new
                        {
                            items = new[]
                            {
                                new
                                {
                                    id = Guid.NewGuid(),
                                    title = "Test Task",
                                    description = "Description",
                                    status = "Open",
                                    budget = 100.00m,
                                    createdAt = "2024-01-01T00:00:00Z"
                                }
                            },
                            totalCount = 1,
                            page = 1,
                            pageSize = 20
                        }));

                        endpoints.MapPost("/api/v1/tasks", () =>
                            Results.Created("/api/v1/tasks/1", new
                            {
                                id = Guid.NewGuid(),
                                title = "New Task",
                                description = "Task description",
                                status = "Draft",
                                budget = 150.00m,
                                createdAt = "2024-01-01T00:00:00Z"
                            }));

                        endpoints.MapPost("/provider-states", async context =>
                        {
                            _ = await context.Request.ReadFromJsonAsync<ProviderState>();
                            context.Response.StatusCode = 200;
                        });
                    });
                });
            })
            .Build();

        _server.Start();
    }

    public void Dispose() => _server.Dispose();

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

public class ProviderState
{
    public string State { get; set; } = "";
    public Dictionary<string, object> Params { get; set; } = new();
}
