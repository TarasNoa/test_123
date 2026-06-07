using System.Net;
using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;
using Libr4.IDE.Application.Obscura;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class AgentStackHealthServiceTests
{
    [Fact]
    public async Task CheckAsync_AllHealthy_ReturnsAllRequiredHealthy()
    {
        var factory = new StubHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = new AgentStackHealthService(
            new FakeObscuraHealth(grpc: true, cdp: true),
            factory,
            Options.Create(new AgentStackOptions
            {
                EnableShadowSyncGate = true,
                EnableSandboxControllerGate = true,
                EnableSecurityScannerGate = true,
                EnableQdrantGate = false
            }),
            NullLogger<AgentStackHealthService>.Instance);

        var status = await sut.CheckAsync();

        status.AllRequiredHealthy.Should().BeTrue();
        status.ObscuraHealthy.Should().BeTrue();
        status.ShadowSyncHealthy.Should().BeTrue();
        status.SandboxControllerHealthy.Should().BeTrue();
        status.SecurityScannerHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAsync_ShadowSyncDown_MarksStackUnhealthy()
    {
        var factory = new StubHttpClientFactory(url =>
            url.Contains("8080", StringComparison.Ordinal)
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK));

        var sut = new AgentStackHealthService(
            new FakeObscuraHealth(grpc: true, cdp: true),
            factory,
            Options.Create(new AgentStackOptions()),
            NullLogger<AgentStackHealthService>.Instance);

        var status = await sut.CheckAsync();

        status.AllRequiredHealthy.Should().BeFalse();
        status.ShadowSyncHealthy.Should().BeFalse();
    }

    private sealed class FakeObscuraHealth : IObscuraHealthService
    {
        private readonly bool _grpc;
        private readonly bool _cdp;

        public FakeObscuraHealth(bool grpc, bool cdp)
        {
            _grpc = grpc;
            _cdp = cdp;
        }

        public Task<ObscuraHealthStatus> CheckAsync(CancellationToken ct = default) =>
            Task.FromResult(new ObscuraHealthStatus(_grpc, _cdp, _grpc ? "grpc" : "cdp", null, null));
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly Func<string, HttpResponseMessage> _responder;

        public StubHttpClientFactory(Func<string, HttpResponseMessage> responder) => _responder = responder;

        public HttpClient CreateClient(string name) =>
            new(new StubHandler(_responder)) { BaseAddress = new Uri("http://localhost/") };

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<string, HttpResponseMessage> _responder;

            public StubHandler(Func<string, HttpResponseMessage> responder) => _responder = responder;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken) =>
                Task.FromResult(_responder(request.RequestUri?.ToString() ?? string.Empty));
        }
    }
}
