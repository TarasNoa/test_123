using Libr4.IDE.Application.AutonomousRuntimePolicy.Commands;
using Libr4.IDE.Application.AutonomousRuntimePolicy.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousRuntimePolicy.Handlers;

/// <summary>
/// Generates a runtime policy by analyzing the prompt for domain signals,
/// evidence requirements, and quality contract thresholds.
/// </summary>
public class GeneratePolicyCommandHandler : IRequestHandler<GeneratePolicyCommand, RuntimePolicyDto>
{
    private readonly ILogger<GeneratePolicyCommandHandler> _logger;

    public GeneratePolicyCommandHandler(ILogger<GeneratePolicyCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task<RuntimePolicyDto> Handle(GeneratePolicyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating runtime policy for workspace {WorkspaceId}", request.WorkspaceId);

        var prompt = request.Prompt.ToLowerInvariant();
        var domainSignal = InferDomainSignal(prompt);
        var runtimeProofRequired = prompt.Contains("production") || prompt.Contains("deploy") || prompt.Contains("release");
        var richAppRequired = prompt.Contains("ui") || prompt.Contains("frontend") || prompt.Contains("blazor") || prompt.Contains("react");

        var qualityChecks = BuildQualityChecks(prompt);

        var policy = new RuntimePolicyDto
        {
            Id = Guid.NewGuid(),
            PolicyId = $"policy-{Guid.NewGuid():N}"[..20],
            Prompt = request.Prompt,
            WorkspaceId = request.WorkspaceId,
            DomainSignal = domainSignal,
            RuntimeEvidenceSignal = runtimeProofRequired ? "runtime-proof-required" : "evidence-optional",
            RuntimeProofRequired = runtimeProofRequired,
            RichAppBuildRequired = richAppRequired,
            QualityContract = new QualityContractDto
            {
                ApprovalRequired = runtimeProofRequired,
                AuditTrailRequired = prompt.Contains("audit") || prompt.Contains("compliance"),
                QualityChecks = qualityChecks,
                ApprovalWorkflow = runtimeProofRequired ? "manual-review" : "auto-approve"
            },
            CreatedAt = DateTime.UtcNow
        };

        return Task.FromResult(policy);
    }

    private static string InferDomainSignal(string prompt)
    {
        if (prompt.Contains("web") || prompt.Contains("api") || prompt.Contains("http")) return "web-domain";
        if (prompt.Contains("data") || prompt.Contains("database") || prompt.Contains("sql")) return "data-domain";
        if (prompt.Contains("ml") || prompt.Contains("ai") || prompt.Contains("model")) return "ai-domain";
        if (prompt.Contains("security") || prompt.Contains("auth") || prompt.Contains("jwt")) return "security-domain";
        if (prompt.Contains("mobile") || prompt.Contains("ios") || prompt.Contains("android")) return "mobile-domain";
        return "general-domain";
    }

    private static List<string> BuildQualityChecks(string prompt)
    {
        var checks = new List<string> { "build-success", "no-compilation-errors" };
        if (prompt.Contains("test")) checks.Add("tests-passing");
        if (prompt.Contains("security")) checks.Add("security-scan-clean");
        if (prompt.Contains("performance")) checks.Add("performance-baseline");
        if (prompt.Contains("docker") || prompt.Contains("container")) checks.Add("container-health-check");
        return checks;
    }
}
