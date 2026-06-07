using FluentAssertions;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Application.Obscura.Commands;
using Libr4.IDE.Application.Obscura.Handlers;
using Libr4.IDE.Application.Obscura.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ObscuraIntegrationTests
{
    [Fact]
    public async Task LaunchNavigateScreenshotClose_FullFlow_UsesStableSessionId()
    {
        var browser = new FakeBrowserAutomationService();
        var service = new ObscuraBrowserServiceAdapter(browser);

        var sessionId = await service.LaunchBrowserAsync(CancellationToken.None);
        sessionId.Should().NotBeNullOrWhiteSpace();
        browser.LaunchedIds.Should().ContainSingle().Which.Should().Be(sessionId);

        await service.NavigateAsync(sessionId, "https://example.com", CancellationToken.None);
        browser.Navigated.Should().ContainSingle();
        browser.Navigated[0].browserId.Should().Be(sessionId);
        browser.Navigated[0].url.Should().Be("https://example.com");

        var screenshot = await service.TakeScreenshotAsync(sessionId, CancellationToken.None);
        screenshot.Should().Equal([0x89, 0x50, 0x4E, 0x47]);

        await service.CloseBrowserAsync(sessionId, CancellationToken.None);
        browser.Closed.Should().ContainSingle().Which.Should().Be(sessionId);

        var info = await service.GetSessionInfoAsync(sessionId);
        info.Should().NotBeNull();
        info!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task MediatRHandlers_ReuseSameSession_AcrossCommands()
    {
        var browser = new FakeBrowserAutomationService();
        var service = new ObscuraBrowserServiceAdapter(browser);

        var launchHandler = new LaunchBrowserCommandHandler(service, NullLogger<LaunchBrowserCommandHandler>.Instance);
        var navigateHandler = new NavigateCommandHandler(service, NullLogger<NavigateCommandHandler>.Instance);
        var screenshotHandler = new TakeScreenshotCommandHandler(service, NullLogger<TakeScreenshotCommandHandler>.Instance);
        var closeHandler = new CloseBrowserCommandHandler(service, NullLogger<CloseBrowserCommandHandler>.Instance);

        var sessionId = await launchHandler.Handle(new LaunchBrowserCommand(), CancellationToken.None);

        await navigateHandler.Handle(
            new NavigateCommand { SessionId = sessionId, Url = "https://example.com" },
            CancellationToken.None);

        var png = await screenshotHandler.Handle(
            new TakeScreenshotCommand { SessionId = sessionId },
            CancellationToken.None);

        png.Should().NotBeEmpty();
        browser.Navigated.Should().ContainSingle().Which.browserId.Should().Be(sessionId);

        await closeHandler.Handle(new CloseBrowserCommand { SessionId = sessionId }, CancellationToken.None);
        browser.Closed.Should().ContainSingle().Which.Should().Be(sessionId);
    }

    [Fact]
    public async Task SessionManager_ReusesLeaseForSameRunId()
    {
        var (manager, browser, dbPath) = CreateSessionStack();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var lease1 = await manager.AcquireAsync(runId, "verify", new ObscuraLaunchOptions(), CancellationToken.None);
            var lease2 = await manager.AcquireAsync(runId, "verify", new ObscuraLaunchOptions(), CancellationToken.None);

            lease2.SessionId.Should().Be(lease1.SessionId);
            browser.LaunchedIds.Should().ContainSingle();
        }
        finally
        {
            TryDeleteFile(dbPath);
        }
    }

    [Fact]
    public async Task SessionJanitor_ClosesExpiredSessions()
    {
        var (manager, browser, dbPath) = CreateSessionStack(o => o.LeaseTimeoutMinutes = 0);
        try
        {
            var lease = await manager.AcquireAsync("run-expired", "test", new ObscuraLaunchOptions(), CancellationToken.None);
            await Task.Delay(50);

            var closed = await manager.CloseExpiredAsync(CancellationToken.None);

            closed.Should().ContainSingle().Which.SessionId.Should().Be(lease.SessionId);
            browser.Closed.Should().ContainSingle().Which.Should().Be(lease.SessionId);
        }
        finally
        {
            TryDeleteFile(dbPath);
        }
    }

    [Fact]
    public async Task Adapter_WithRunId_ReusesManagedSession()
    {
        var (manager, browser, dbPath) = CreateSessionStack();
        try
        {
            var service = new ObscuraBrowserServiceAdapter(browser, manager);
            var runId = Guid.NewGuid().ToString("N");

            var session1 = await service.LaunchBrowserAsync(new ObscuraLaunchOptions { RunId = runId }, CancellationToken.None);
            var session2 = await service.LaunchBrowserAsync(new ObscuraLaunchOptions { RunId = runId }, CancellationToken.None);

            session2.Should().Be(session1);
            browser.LaunchedIds.Should().ContainSingle();
        }
        finally
        {
            TryDeleteFile(dbPath);
        }
    }

    [Fact]
    public void DomToMarkdownConverter_StripsNoiseAndPreservesLinks()
    {
        var converter = new DomToMarkdownConverter();
        var html = """
            <html><body>
            <nav>skip me</nav>
            <main><h1>Title</h1><p>Read <a href="https://example.com">docs</a>.</p></main>
            </body></html>
            """;

        var markdown = converter.Convert(html, new ConversionOptions { RemoveNoise = true, IncludeLinks = true });

        markdown.Should().Contain("Title");
        markdown.Should().Contain("https://example.com");
        markdown.Should().NotContain("skip me");
    }

    private static (ObscuraSessionManager Manager, FakeBrowserAutomationService Browser, string DbPath) CreateSessionStack(
        Action<ObscuraSessionOptions>? configure = null)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"obscura-sessions-{Guid.NewGuid():N}.db");
        var options = new ObscuraSessionOptions
        {
            DbPath = dbPath,
            MaxConcurrentSessions = 5,
            PortRangeStart = 9222,
            PortRangeEnd = 9225,
            LeaseTimeoutMinutes = 30
        };
        configure?.Invoke(options);

        var browser = new FakeBrowserAutomationService();
        var repository = new SqliteObscuraSessionRepository(
            Options.Create(options),
            NullLogger<SqliteObscuraSessionRepository>.Instance);
        repository.EnsureSchemaAsync().GetAwaiter().GetResult();

        var manager = new ObscuraSessionManager(
            browser,
            repository,
            Options.Create(options),
            NullLogger<ObscuraSessionManager>.Instance);

        return (manager, browser, dbPath);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    private sealed class FakeBrowserAutomationService : IBrowserAutomationService
    {
        public List<string> LaunchedIds { get; } = [];
        public List<(string browserId, string url)> Navigated { get; } = [];
        public List<string> Closed { get; } = [];

        public Task<string> LaunchBrowserAsync(
            string browserId,
            bool headless = true,
            string? userAgent = null,
            CancellationToken ct = default)
        {
            LaunchedIds.Add(browserId);
            return Task.FromResult($"ws://localhost/{browserId}");
        }

        public Task CloseBrowserAsync(string browserId, CancellationToken ct = default)
        {
            Closed.Add(browserId);
            return Task.CompletedTask;
        }

        public Task<NavigationResult> NavigateAsync(
            string browserId,
            string url,
            bool waitUntilNetworkIdle = true,
            CancellationToken ct = default)
        {
            Navigated.Add((browserId, url));
            return Task.FromResult(new NavigationResult
            {
                Success = true,
                FinalUrl = url,
                StatusCode = 200
            });
        }

        public Task<string> GetPageSourceAsync(string browserId, CancellationToken ct = default)
            => Task.FromResult("<html><body>ok</body></html>");

        public Task<string> GetPageTextAsync(string browserId, CancellationToken ct = default)
            => Task.FromResult("ok");

        public Task<byte[]> TakeScreenshotAsync(
            string browserId,
            bool fullPage = false,
            string? selector = null,
            CancellationToken ct = default)
            => Task.FromResult<byte[]>([0x89, 0x50, 0x4E, 0x47]);

        public Task<string> ExtractElementAsync(
            string browserId,
            string selector,
            ExtractionType extractionType = ExtractionType.Text,
            string? attributeName = null,
            CancellationToken ct = default)
            => Task.FromResult("");

        public Task<List<string>> ExtractMultipleAsync(
            string browserId,
            string selector,
            ExtractionType extractionType = ExtractionType.Text,
            string? attributeName = null,
            CancellationToken ct = default)
            => Task.FromResult(new List<string>());

        public Task<T> ExecuteJavaScriptAsync<T>(string browserId, string script, CancellationToken ct = default)
            => Task.FromResult(default(T)!);

        public Task ClickElementAsync(string browserId, string selector, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task TypeTextAsync(
            string browserId,
            string selector,
            string text,
            bool clearFirst = true,
            CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<MarkdownConversionResult> ConvertToMarkdownAsync(
            string browserId,
            bool includeImages = true,
            CancellationToken ct = default)
            => Task.FromResult(new MarkdownConversionResult { Markdown = "# ok" });

        public Task CrawlAsync(
            string browserId,
            string startUrl,
            int maxPages = 10,
            int maxDepth = 2,
            Action<CrawlEvent>? onEvent = null,
            CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
