using Libr4.IDE.Application.SecurityTesting;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.IDE.Api;

/// <summary>
/// Minimal API endpoints for Security Testing
/// </summary>
public static class SecurityTestingEndpoints
{
    public static void MapSecurityTestingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/security-testing")
            .WithTags("Security Testing")
            .RequireAuthorization();

        group.MapPost("/scan", async (
            [FromBody] SecurityScanRequest request,
            ISecurityTestingService service,
            CancellationToken ct) =>
        {
            var result = await service.RunTestsAsync(
                request.TargetPath,
                new SecurityTestOptions
                {
                    RunStaticAnalysis = request.RunStaticAnalysis,
                    RunDependencyScan = request.RunDependencyScan,
                    RunSecretsScan = request.RunSecretsScan,
                    ExcludePatterns = request.ExcludePatterns
                }, ct);
            return Results.Ok(result);
        })
        .WithName("RunSecurityScan")
        .WithSummary("Run security scan on target path");

        group.MapPost("/scan-dependencies", async (
            [FromBody] DependencyScanRequest request,
            ISecurityTestingService service,
            CancellationToken ct) =>
        {
            var result = await service.ScanDependenciesAsync(request.ProjectPath, ct);
            return Results.Ok(result);
        })
        .WithName("ScanDependencies")
        .WithSummary("Scan project dependencies for known vulnerabilities");
    }
}

public record SecurityScanRequest(
    string TargetPath,
    bool RunStaticAnalysis = true,
    bool RunDependencyScan = true,
    bool RunSecretsScan = true,
    string[]? ExcludePatterns = null);

public record DependencyScanRequest(string ProjectPath);
