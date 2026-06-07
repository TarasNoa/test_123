using Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Api;

public static class GitHubCiWebhookEndpoints
{
    public static void MapGitHubCiWebhookEndpoints(this IEndpointRouteBuilder app, string routePrefix = "/api/v1/ide/webhooks/github")
    {
        var group = app.MapGroup(routePrefix).WithTags("GitHub Webhooks");

        group.MapPost("/ci", async (
            HttpContext http,
            IGitHubCiWebhookService webhook,
            CancellationToken ct) =>
        {
            using var reader = new StreamReader(http.Request.Body);
            var body = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
            var signature = http.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            var handled = await webhook.HandleAsync(signature, body, ct).ConfigureAwait(false);
            return handled ? Results.Ok(new { accepted = true }) : Results.Unauthorized();
        })
        .WithName("GitHubCiWebhook");
    }
}
