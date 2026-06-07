using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ShadowSyncClientTests
{
    [Fact]
    public async Task TriggerSync_WhenSyncEndpointMissing_StillSucceedsAfterHealth()
    {
        var factory = new StubHttpClientFactory(url =>
        {
            if (url.EndsWith("/health", StringComparison.Ordinal))
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            if (url.EndsWith("/sync", StringComparison.Ordinal))
                return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        });

        var client = factory.CreateClient("test");
        client.BaseAddress = new Uri("http://shadow-sync:8080/");
        var sut = new ShadowSyncClient(
            client,
            Options.Create(new AgentStackOptions { EnableShadowSyncGate = true }),
            NullLogger<ShadowSyncClient>.Instance);

        var ok = await sut.TriggerSyncAsync("workspace-1");

        ok.Should().BeTrue();
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly Func<string, HttpResponseMessage> _responder;

        public StubHttpClientFactory(Func<string, HttpResponseMessage> responder) => _responder = responder;

        public HttpClient CreateClient(string name) =>
            new(new StubHandler(_responder));

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
