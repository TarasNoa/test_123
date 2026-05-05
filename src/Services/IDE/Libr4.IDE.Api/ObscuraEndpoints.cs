using Libr4.IDE.Application.Obscura.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Obscura browser integration
/// </summary>
public static class ObscuraEndpoints
{
    public static void MapObscuraEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/obscura")
            .WithTags("Obscura Browser")
            .RequireAuthorization();
        
        group.MapPost("/launch", async (
            IMediator mediator,
            CancellationToken ct) =>
        {
            var sessionId = await mediator.Send(new LaunchBrowserCommand(), ct);
            return Results.Ok(new { sessionId });
        })
        .WithName("LaunchObscuraBrowser")
        .WithSummary("Launch Obscura browser")
        .WithDescription("Launches a new Obscura browser instance for AI agents (30MB RAM, 85ms page load)");
        
        group.MapPost("/navigate", async (
            [FromBody] NavigateCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(command, ct);
            return Results.Ok(new { success = true });
        })
        .WithName("NavigateObscuraBrowser")
        .WithSummary("Navigate to URL")
        .WithDescription("Navigates the Obscura browser to a specified URL");
        
        group.MapPost("/screenshot", async (
            [FromBody] TakeScreenshotCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var screenshot = await mediator.Send(command, ct);
            return Results.File(screenshot, "image/png");
        })
        .WithName("TakeObscuraScreenshot")
        .WithSummary("Take screenshot")
        .WithDescription("Takes a screenshot of the current page in the Obscura browser");
        
        group.MapPost("/execute-js", async (
            [FromBody] ExecuteJavaScriptCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(new { result });
        })
        .WithName("ExecuteJavaScriptInObscura")
        .WithSummary("Execute JavaScript")
        .WithDescription("Executes JavaScript code in the Obscura browser context");
        
        group.MapPost("/content", async (
            [FromBody] GetPageContentCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var content = await mediator.Send(command, ct);
            return Results.Ok(new { content });
        })
        .WithName("GetObscuraPageContent")
        .WithSummary("Get page content")
        .WithDescription("Retrieves the HTML content of the current page");
        
        group.MapPost("/close", async (
            [FromBody] CloseBrowserCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(command, ct);
            return Results.Ok(new { success = true });
        })
        .WithName("CloseObscuraBrowser")
        .WithSummary("Close browser")
        .WithDescription("Closes the Obscura browser instance");
        
        group.MapPost("/wait", async (
            [FromBody] WaitForElementCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(command, ct);
            return Results.Ok(new { success = true });
        })
        .WithName("WaitForElementInObscura")
        .WithSummary("Wait for element")
        .WithDescription("Waits for an element to appear on the page");
        
        group.MapPost("/click", async (
            [FromBody] ClickCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(command, ct);
            return Results.Ok(new { success = true });
        })
        .WithName("ClickElementInObscura")
        .WithSummary("Click element")
        .WithDescription("Clicks on an element in the Obscura browser");
        
        group.MapPost("/type", async (
            [FromBody] TypeCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(command, ct);
            return Results.Ok(new { success = true });
        })
        .WithName("TypeInObscura")
        .WithSummary("Type text")
        .WithDescription("Types text into an element in the Obscura browser");
    }
}
