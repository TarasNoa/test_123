namespace Libr4.IDE.Api;

public static class GitHubBootstrapEndpoints
{
    public static void MapGitHubBootstrapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/githubbootstrap")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "GitHubBootstrap" }));
    }
}
