using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ObscuraNetworkRouterTests : IDisposable
{
    private readonly ObscuraNetworkRouter _router;

    public ObscuraNetworkRouterTests()
    {
        _router = new ObscuraNetworkRouter(Options.Create(new ObscuraNetworkRouterOptions
        {
            DockerBrowserHost = "host.docker.internal",
            UseDockerHostMapping = true,
            ReadinessMaxAttempts = 8,
            ReadinessPollIntervalMs = 100,
            ReadinessRequestTimeoutSeconds = 2
        }));
    }

    public void Dispose()
    {
    }

    [Fact]
    public void Resolve_Backend_ReturnsDockerHostUrl()
    {
        var runId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();

        _router.RegisterWorkspace(workspaceId, 4001);
        _router.BindRun(runId, workspaceId);
        _router.RegisterService(runId, "backend", 8000, "/");

        _router.TryResolve(runId, "backend", out var url).Should().BeTrue();
        url.Should().Be("http://host.docker.internal:8000/");
    }

    [Fact]
    public void ResolveForBrowser_RewritesLocalhost()
    {
        var runId = Guid.NewGuid();
        _router.ResolveForBrowser(runId, "http://localhost:5173/app")
            .Should().Be("http://host.docker.internal:5173/app");
    }

    [Fact]
    public void RegisterServices_SupportsFrontendBackendApiAliases()
    {
        var runId = Guid.NewGuid();
        _router.RegisterServices(runId,
        [
            new ObscuraServiceRegistration("backend", 8000, "/"),
            new ObscuraServiceRegistration("frontend", 5173, "/"),
            new ObscuraServiceRegistration("api", 8080, "/health")
        ]);

        _router.TryResolve(runId, "frontend", out var frontend).Should().BeTrue();
        _router.TryResolve(runId, "api", out var api).Should().BeTrue();
        frontend.Should().Be("http://host.docker.internal:5173/");
        api.Should().Be("http://host.docker.internal:8080/health");
    }

    [Fact]
    public async Task ReadinessProbe_PollsUntil200()
    {
        await using var server = await StartLoopbackHttpServerAsync();
        var probe = new ObscuraReadinessProbeService(
            _router,
            Options.Create(new ObscuraNetworkRouterOptions
            {
                ReadinessMaxAttempts = 8,
                ReadinessPollIntervalMs = 100,
                ReadinessRequestTimeoutSeconds = 2
            }),
            NullLogger<ObscuraReadinessProbeService>.Instance);

        var result = await probe.ProbeUrlAsync(server.BaseUrl);

        result.Ready.Should().BeTrue();
        result.Attempts.Should().NotBeEmpty();
        result.Attempts.Last().StatusCode.Should().Be(200);
    }

    private static async Task<LoopbackServer> StartLoopbackHttpServerAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cts.Token);
                }
                catch
                {
                    break;
                }

                await using var stream = client.GetStream();
                var buffer = new byte[1024];
                _ = await stream.ReadAsync(buffer, cts.Token);
                var response = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\nok"u8.ToArray();
                await stream.WriteAsync(response, cts.Token);
            }
        }, cts.Token);

        return new LoopbackServer(listener, cts, port);
    }

    private sealed class LoopbackServer(TcpListener listener, CancellationTokenSource cts, int port) : IAsyncDisposable
    {
        public string BaseUrl => $"http://127.0.0.1:{port}/";

        public async ValueTask DisposeAsync()
        {
            await cts.CancelAsync();
            listener.Stop();
            cts.Dispose();
        }
    }
}
