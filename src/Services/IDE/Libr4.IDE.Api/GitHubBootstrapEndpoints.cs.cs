/*
using Libr4.IDE.Application.GitHubBootstrap.Commands;
using Libr4.IDE.Application.GitHubBootstrap.Validators;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for GitHub Bootstrap
/// </summary>
public static class GitHubBootstrapEndpoints
{
    public static void MapGitHubBootstrapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/github-bootstrap")
            .WithTags("GitHub Bootstrap")
            .RequireAuthorization()
            .WithOpenApi();
        
        group.MapPost("/bootstrap", async (
            [FromBody] BootstrapProjectCommand command,
            IMediator mediator,
            BootstrapProjectCommandValidator validator,
            CancellationToken ct) =>
        {
            // Validate command
            var validationResult = await validator.ValidateAsync(command, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }
            
            // Execute command
            var result = await mediator.Send(command, ct);
            
            return Results.Ok(result);
        })
        .WithName("BootstrapProject")
        .WithSummary("Bootstrap project from GitHub template")
        .WithDescription("Searches GitHub for suitable project templates, checks licenses, and seeds new projects with template code")
        .WithOpenApi();
    }
}
*/
