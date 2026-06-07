using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.LiveSearch;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class LiveSearchTests
{
    [Fact]
    public void SsrfGuard_BlocksLocalhostQuery()
    {
        SearchSsrfGuard.IsQuerySafe("django docs").Should().BeTrue();
        SearchSsrfGuard.IsQuerySafe("fetch http://127.0.0.1/admin").Should().BeFalse();
        SearchSsrfGuard.IsQuerySafe("metadata 169.254.169.254").Should().BeFalse();
    }

    [Fact]
    public void SsrfGuard_BlocksPrivateIpTargets()
    {
        SearchSsrfGuard.IsBlockedTarget(new Uri("http://10.0.0.5/internal")).Should().BeTrue();
        SearchSsrfGuard.IsBlockedTarget(new Uri("https://example.com/docs")).Should().BeFalse();
    }

    [Fact]
    public void RateLimiter_EnforcesMaxRequestsPerMinute()
    {
        var limiter = new LiveSearchRateLimiter(Options.Create(new LiveSearchOptions { MaxRequestsPerMinute = 2 }));
        limiter.TryAcquire("session-a").Should().BeTrue();
        limiter.TryAcquire("session-a").Should().BeTrue();
        limiter.TryAcquire("session-a").Should().BeFalse();
        limiter.TryAcquire("session-b").Should().BeTrue();
    }

    [Fact]
    public void Cache_ReturnsCachedResponse()
    {
        var cache = new LiveSearchCache(Options.Create(new LiveSearchOptions { CacheTtlSeconds = 600 }));
        var key = LiveSearchCache.BuildKey("duckduckgo", "libr4 agents", 5);
        var response = new LiveSearchResponse("libr4 agents", "duckduckgo", false, [
            new LiveSearchHit("Libr4", "https://example.com", "snippet", "duckduckgo")
        ]);

        cache.Set(key, response);
        cache.TryGet(key, out var cached).Should().BeTrue();
        cached.FromCache.Should().BeTrue();
        cached.Hits.Should().HaveCount(1);
    }

    [Fact]
    public void ContentTruncator_CapsResponseSize()
    {
        var hits = Enumerable.Range(0, 20)
            .Select(i => new LiveSearchHit($"title-{i}", $"https://example.com/{i}", new string('x', 800), "duckduckgo"))
            .ToList();
        var truncated = SearchContentTruncator.Truncate(
            new LiveSearchResponse("query", "duckduckgo", false, hits),
            maxSnippetChars: 200,
            maxResponseChars: 2000);

        truncated.Hits.Count.Should().BeLessThan(hits.Count);
        truncated.TruncationNotice.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SearchWeb_UsesCacheOnSecondCall()
    {
        var service = BuildService("""
            {
              "AbstractText": "Libr4 agent platform",
              "Heading": "Libr4",
              "AbstractURL": "https://example.com/libr4",
              "RelatedTopics": []
            }
            """);

        var request = new LiveSearchRequest("libr4 platform", "cache-test", 5);
        var first = await service.SearchWebAsync(request);
        var second = await service.SearchWebAsync(request);

        first.FromCache.Should().BeFalse();
        second.FromCache.Should().BeTrue();
        second.Hits.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchX_WhenDisabled_ReturnsError()
    {
        var service = BuildService("{}");
        var act = () => service.SearchXAsync(new LiveSearchRequest("cursor agents", "x-test", 3));
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("search_x_disabled");
    }

    private static LiveSearchService BuildService(string ddgJson)
    {
        var handler = new FakeJsonHandler(ddgJson);
        var http = new HttpClient(handler);
        var options = Options.Create(new LiveSearchOptions
        {
            Enabled = true,
            MaxRequestsPerMinute = 30,
            CacheTtlSeconds = 600,
            EnableSearchX = false
        });

        return new LiveSearchService(
            options,
            new LiveSearchRateLimiter(options),
            new LiveSearchCache(options),
            new DuckDuckGoLiveSearchBackend(http, NullLogger<DuckDuckGoLiveSearchBackend>.Instance),
            new BraveLiveSearchBackend(http, options, NullLogger<BraveLiveSearchBackend>.Instance),
            new XLiveSearchBackend(http, options, NullLogger<XLiveSearchBackend>.Instance),
            NullLogger<LiveSearchService>.Instance);
    }

    private sealed class FakeJsonHandler : HttpMessageHandler
    {
        private readonly string _json;

        public FakeJsonHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
            });
    }
}
