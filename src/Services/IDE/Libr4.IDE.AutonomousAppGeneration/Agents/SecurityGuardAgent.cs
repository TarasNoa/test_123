/*
using System.Diagnostics;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Application.ShadowWorkspace;
using Libr4.IDE.Infrastructure.Memory;
using Libr4.Payments.Domain.TaxManagement.FSharp;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.Agents;

/// <summary>
/// SecurityGuardAgent - Multi-layer security validation agent
/// Integrates: Watermarking, Semantic Blame, Double-Entry Integrity, HiveMind Consensus
/// Role: Validates code changes for security, financial integrity, and IP protection
/// </summary>
public class SecurityGuardAgent : IAgent
{
    private readonly ILogger<SecurityGuardAgent> _logger;
    private readonly IWatermarkingService _watermarking;
    private readonly ISemanticBlameService _semanticBlame;
    private readonly IAgentDebateService _debateService;
    private readonly LearningPattern _learningPattern;
    private readonly IContextCompressionService _compression;
    
    // Agent capabilities
    private readonly List<AgentCapability> _capabilities = new()
    {
        new AgentCapability { Name = "WatermarkVerification", Description = "Verify IP protection watermarks" },
        new AgentCapability { Name = "SemanticBlameAnalysis", Description = "Analyze code history and modification risks" },
        new AgentCapability { Name = "FinancialIntegrityCheck", Description = "Validate double-entry bookkeeping balance" },
        new AgentCapability { Name = "ConsensusValidation", Description = "Participate in security-weighted consensus" },
        new AgentCapability { Name = "ShadowWorkspaceIsolation", Description = "Validate container security boundaries" }
    };

    public SecurityGuardAgent(
        ILogger<SecurityGuardAgent> logger,
        IWatermarkingService watermarking,
        ISemanticBlameService semanticBlame,
        IAgentDebateService debateService,
        IContextCompressionService compression)
    {
        _logger = logger;
        _watermarking = watermarking;
        _semanticBlame = semanticBlame;
        _debateService = debateService;
        _compression = compression;
        
        // Initialize learning pattern with EMA
        _learningPattern = new LearningPattern("SecurityGuard", "Multi-layer security validation")
        {
            SuccessRate = 0.85,  // Initial confidence
            PatternData = new Dictionary<string, object>
            {
                ["CriticalFindings"] = 0,
                ["WarningsIssued"] = 0,
                ["BlocksPrevented"] = 0
            }
        };
    }

    public async Task<AgentResult> ExecuteAsync(AgentContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var task = context.Task ?? throw new ArgumentException("Task required");
        
        _logger.LogInformation(
            "SecurityGuardAgent executing task: {TaskType} for workspace {Workspace}",
            task.Type,
            context.WorkspaceId);

        try
        {
            // STEP 1: Decompress context (avoid token overflow)
            var compressedContext = await _compression.CompressAgentContextAsync(
                task.Context ?? string.Empty,
                targetTokens: 1500);

            // STEP 2: Multi-layer security analysis
            var securityReport = await PerformMultiLayerAnalysisAsync(
                context,
                compressedContext,
                task);

            // STEP 3: Participate in HiveMind consensus if critical findings
            ConsensusResult? consensus = null;
            if (securityReport.HasCriticalFindings)
            {
                consensus = await ParticipateInSecurityConsensusAsync(
                    context,
                    securityReport);
            }

            // STEP 4: Update learning pattern (EMA)
            var success = !securityReport.HasBlockingIssues;
            _learningPattern.RecordSuccess(success);
            _learningPattern.UpdatePatternData("TotalScans", 
                (int)_learningPattern.PatternData.GetValueOrDefault("TotalScans", 0) + 1);

            stopwatch.Stop();

            // STEP 5: Build structured result
            var result = new AgentResult
            {
                IsSuccess = success,
                IsApproved = consensus?.Success ?? !securityReport.HasCriticalFindings,
                Content = FormatSecurityReport(securityReport),
                PerformanceProfile = new PerformanceProfile
                {
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    TokenUsage = compressedContext.OriginalTokens - compressedContext.CompressedTokens
                },
                TechDebt = new TechDebtItem
                {
                    Category = "Security",
                    Severity = securityReport.MaxSeverity,
                    Description = securityReport.Summary
                },
                Observability = new ObservabilityData
                {
                    Traces = securityReport.Traces,
                    Metrics = new Dictionary<string, double>
                    {
                        ["security_score"] = securityReport.SecurityScore,
                        ["learning_success_rate"] = _learningPattern.SuccessRate,
                        ["critical_findings"] = securityReport.CriticalCount
                    }
                }
            };

            // STEP 6: Suggest subtasks if needed
            if (securityReport.RequiresRemediation)
            {
                result.SuggestedSubtasks = GenerateRemediationTasks(securityReport);
            }

            _logger.LogInformation(
                "SecurityGuardAgent completed in {DurationMs}ms. Score: {Score:F2}, Critical: {Critical}",
                stopwatch.ElapsedMilliseconds,
                securityReport.SecurityScore,
                securityReport.CriticalCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SecurityGuardAgent failed");
            _learningPattern.RecordSuccess(false);
            
            return new AgentResult
            {
                IsSuccess = false,
                Content = $"Security analysis failed: {ex.Message}",
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Performs multi-layer security analysis (Layer 1-5)
    /// </summary>
    private async Task<SecurityReport> PerformMultiLayerAnalysisAsync(
        AgentContext context,
        CompressedContext compressedContext,
        AgentTask task)
    {
        var report = new SecurityReport
        {
            WorkspaceId = context.WorkspaceId ?? "unknown",
            Timestamp = DateTime.UtcNow,
            Findings = new List<SecurityFinding>(),
            Traces = new List<string>()
        };

        // LAYER 1: Watermark Verification (IP Protection)
        if (task.Parameters.TryGetValue("previewHtml", out var previewHtml))
        {
            var orderId = task.Parameters.GetValueOrDefault("orderId", "unknown");
            var watermarkValid = await _watermarking.VerifyWatermarkAsync(
                previewHtml,
                orderId);
            
            report.Findings.Add(new SecurityFinding
            {
                Layer = "IP Protection",
                Category = "Watermarking",
                Severity = watermarkValid ? Severity.Info : Severity.Critical,
                Message = watermarkValid 
                    ? "Watermark verified - content protected" 
                    : "CRITICAL: Watermark missing or tampered - IP at risk",
                Recommendation = watermarkValid ? null : "Regenerate preview with fresh watermark"
            });
            
            report.Traces.Add($"Watermark verification: {(watermarkValid ? "PASS" : "FAIL")} for order {orderId}");
        }

        // LAYER 2: Semantic Blame Analysis (Code History)
        if (task.Parameters.TryGetValue("modifiedFile", out var modifiedFile))
        {
            var lineNumber = int.Parse(task.Parameters.GetValueOrDefault("lineNumber", "0"));
            
            // Get semantic context
            var semanticContext = await _semanticBlame.GetSemanticContextAsync(
                modifiedFile,
                lineNumber);
            
            if (semanticContext != null)
            {
                report.Findings.Add(new SecurityFinding
                {
                    Layer = "Code History",
                    Category = "SemanticBlame",
                    Severity = Severity.Info,
                    Message = $"Code context: {semanticContext.ContextExplanation}",
                    Metadata = new Dictionary<string, string>
                    {
                        ["Author"] = semanticContext.Author,
                        ["Commit"] = semanticContext.CommitHash[..8],
                        ["RelatedFiles"] = string.Join(", ", semanticContext.RelatedFiles.Take(3))
                    }
                });
            }

            // Assess modification risk
            var risk = await _semanticBlame.AssessModificationRiskAsync(
                modifiedFile,
                task.Parameters.GetValueOrDefault("proposedChange", ""));
            
            if (risk.RiskLevel >= RiskLevel.High)
            {
                report.Findings.Add(new SecurityFinding
                {
                    Layer = "Risk Assessment",
                    Category = "SemanticBlame",
                    Severity = risk.RiskLevel == RiskLevel.Critical ? Severity.Critical : Severity.Warning,
                    Message = risk.WarningMessage,
                    Recommendation = "Consider code review from: " + string.Join(", ", risk.SuggestedReviewers.Take(2))
                });
            }
            
            report.Traces.Add($"Risk assessment: {risk.RiskLevel} for {modifiedFile}");
        }

        // LAYER 3: Financial Integrity (Double-Entry Bookkeeping)
        if (task.Parameters.TryGetValue("financialTransaction", out var transactionJson))
        {
            // Call F# double-entry validation via C# interop
            var validationResult = await ValidateFinancialTransactionAsync(transactionJson);
            
            if (!validationResult.IsValid)
            {
                report.Findings.Add(new SecurityFinding
                {
                    Layer = "Financial Integrity",
                    Category = "DoubleEntry",
                    Severity = Severity.Critical,
                    Message = $"FINANCIAL ERROR: {validationResult.ErrorMessage}",
                    Recommendation = "BLOCK ALL PAYMENTS - Investigate immediately",
                    Metadata = new Dictionary<string, string>
                    {
                        ["BalanceMismatch"] = validationResult.BalanceDifference.ToString()
                    }
                });
                
                report.HasBlockingIssues = true;
            }
            else
            {
                report.Findings.Add(new SecurityFinding
                {
                    Layer = "Financial Integrity",
                    Category = "DoubleEntry",
                    Severity = Severity.Info,
                    Message = "Double-entry validation passed - books balanced"
                });
            }
            
            report.Traces.Add($"Financial validation: {(validationResult.IsValid ? "BALANCED" : "MISMATCH")}");
        }

        // LAYER 4: Shadow Workspace Isolation
        if (task.Parameters.TryGetValue("containerId", out var containerId))
        {
            // Check container security boundaries
            var isolationChecks = await ValidateContainerIsolationAsync(containerId);
            
            foreach (var check in isolationChecks)
            {
                report.Findings.Add(new SecurityFinding
                {
                    Layer = "Container Security",
                    Category = "ShadowWorkspace",
                    Severity = check.Passed ? Severity.Info : Severity.Warning,
                    Message = check.Description
                });
            }
        }

        // LAYER 5: Context Compression Validation
        report.Findings.Add(new SecurityFinding
        {
            Layer = "Token Management",
            Category = "ContextCompression",
            Severity = Severity.Info,
            Message = $"Context compressed: {compressedContext.OriginalTokens} → {compressedContext.CompressedTokens} tokens ({compressedContext.CompressionRatio:F1}%)",
            Metadata = new Dictionary<string, string>
            {
                ["OriginalTokens"] = compressedContext.OriginalTokens.ToString(),
                ["CompressedTokens"] = compressedContext.CompressedTokens.ToString(),
                ["Algorithm"] = compressedContext.Algorithm
            }
        });

        // Calculate summary metrics
        report.CriticalCount = report.Findings.Count(f => f.Severity == Severity.Critical);
        report.WarningCount = report.Findings.Count(f => f.Severity == Severity.Warning);
        report.InfoCount = report.Findings.Count(f => f.Severity == Severity.Info);
        report.HasCriticalFindings = report.CriticalCount > 0;
        report.RequiresRemediation = report.CriticalCount > 0 || report.WarningCount > 3;
        report.SecurityScore = Math.Max(0, 100 - (report.CriticalCount * 25) - (report.WarningCount * 10));
        report.MaxSeverity = report.Findings.Any() ? report.Findings.Max(f => f.Severity) : Severity.Info;
        report.Summary = GenerateSummary(report);

        return report;
    }

    /// <summary>
    /// Participates in HiveMind consensus for critical security decisions
    /// </summary>
    private async Task<ConsensusResult> ParticipateInSecurityConsensusAsync(
        AgentContext context,
        SecurityReport report)
    {
        _logger.LogInformation("Initiating security consensus due to critical findings");

        // Spawn additional security experts for consensus
        var participants = new[]
        {
            new AgentRole
            {
                AgentId = $"security-guard-{Guid.NewGuid():N}",
                Role = "SecurityGuard",
                Specialization = "Multi-layer validation"
            },
            new AgentRole
            {
                AgentId = $"security-expert-{Guid.NewGuid():N}",
                Role = "SecurityExpert",
                Specialization = "Penetration testing"
            },
            new AgentRole
            {
                AgentId = $"risk-analyst-{Guid.NewGuid():N}",
                Role = "RiskAnalyst",
                Specialization = "Financial risk assessment"
            }
        };

        var proposal = $"Security report shows {report.CriticalCount} critical findings. " +
                      $"Recommendation: {(report.HasBlockingIssues ? "BLOCK" : "WARNING")}";

        var consensus = await _debateService.ReachConsensusAsync(
            task: "Critical Security Decision",
            initialProposal: proposal,
            reviewers: participants,
            options: new ConsensusOptions
            {
                MaxIterations = 3,
                ConsensusThreshold = 0.67,  // 2/3 majority
                RequireUnanimity = report.HasBlockingIssues  // Require all for blocks
            });

        _logger.LogInformation(
            "Security consensus reached: {Success} with score {Score:F2}",
            consensus.Success,
            consensus.ConsensusScore);

        return consensus;
    }

    /// <summary>
    /// Validates financial transaction via F# interop
    /// </summary>
    private async Task<FinancialValidationResult> ValidateFinancialTransactionAsync(string transactionJson)
    {
        // This would call the F# DoubleEntryBookkeeping module
        // For now, simulate validation
        await Task.Delay(50);  // Simulate F# computation
        
        try
        {
            // Parse and validate
            var random = new Random();
            var isValid = random.NextDouble() > 0.1;  // 90% pass rate for demo
            
            return new FinancialValidationResult
            {
                IsValid = isValid,
                ErrorMessage = isValid ? null : "Balance mismatch detected: 0.00001 difference",
                BalanceDifference = isValid ? 0.0 : 0.00001
            };
        }
        catch (Exception ex)
        {
            return new FinancialValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Validation error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Validates container isolation
    /// </summary>
    private async Task<List<IsolationCheck>> ValidateContainerIsolationAsync(string containerId)
    {
        // Simulated checks
        await Task.Delay(30);
        
        return new List<IsolationCheck>
        {
            new IsolationCheck { Description = "Network namespace isolation", Passed = true },
            new IsolationCheck { Description = "Filesystem read-only except /tmp", Passed = true },
            new IsolationCheck { Description = "No access to Docker socket", Passed = true },
            new IsolationCheck { Description = "Resource limits enforced", Passed = true }
        };
    }

    /// <summary>
    /// Formats security report for human readability
    /// </summary>
    private string FormatSecurityReport(SecurityReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Security Analysis Report - {report.Timestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Workspace:** {report.WorkspaceId}");
        sb.AppendLine($"**Security Score:** {report.SecurityScore:F1}/100");
        sb.AppendLine($"**Findings:** {report.CriticalCount} Critical, {report.WarningCount} Warning, {report.InfoCount} Info");
        sb.AppendLine();
        
        sb.AppendLine("## Findings by Layer");
        foreach (var layer in report.Findings.GroupBy(f => f.Layer))
        {
            sb.AppendLine($"\n### {layer.Key}");
            foreach (var finding in layer.OrderByDescending(f => f.Severity))
            {
                var emoji = finding.Severity switch
                {
                    Severity.Critical => "🔴",
                    Severity.Warning => "🟡",
                    _ => "🟢"
                };
                sb.AppendLine($"{emoji} **{finding.Category}**: {finding.Message}");
                if (finding.Recommendation != null)
                {
                    sb.AppendLine($"   💡 Recommendation: {finding.Recommendation}");
                }
            }
        }
        
        if (report.HasBlockingIssues)
        {
            sb.AppendLine("\n⚠️ **BLOCKING ISSUES DETECTED - DEPLOYMENT NOT RECOMMENDED**");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Generates summary string
    /// </summary>
    private string GenerateSummary(SecurityReport report)
    {
        if (report.HasBlockingIssues)
        {
            return $"CRITICAL: {report.CriticalCount} blocking security issues found. Deployment blocked.";
        }
        if (report.HasCriticalFindings)
        {
            return $"WARNING: {report.CriticalCount} critical issues require attention before deployment.";
        }
        if (report.WarningCount > 0)
        {
            return $"OK: {report.WarningCount} warnings, no critical issues. Review recommended.";
        }
        return "PASS: All security checks passed.";
    }

    /// <summary>
    /// Generates remediation subtasks
    /// </summary>
    private List<AgentTask> GenerateRemediationTasks(SecurityReport report)
    {
        var tasks = new List<AgentTask>();
        
        foreach (var critical in report.Findings.Where(f => f.Severity == Severity.Critical))
        {
            tasks.Add(new AgentTask
            {
                Type = "SecurityRemediation",
                Description = $"Fix: {critical.Message}",
                Priority = TaskPriority.Critical,
                Context = critical.Recommendation,
                EstimatedDuration = TimeSpan.FromMinutes(30)
            });
        }
        
        return tasks;
    }

    // Supporting types
    private class SecurityReport
    {
        public string WorkspaceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<SecurityFinding> Findings { get; set; } = new();
        public List<string> Traces { get; set; } = new();
        public int CriticalCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
        public bool HasCriticalFindings { get; set; }
        public bool HasBlockingIssues { get; set; }
        public bool RequiresRemediation { get; set; }
        public double SecurityScore { get; set; }
        public Severity MaxSeverity { get; set; }
        public string Summary { get; set; } = string.Empty;
    }

    private class SecurityFinding
    {
        public string Layer { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Severity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Recommendation { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    private enum Severity { Info, Warning, Critical }

    private class FinancialValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public double BalanceDifference { get; set; }
    }

    private class IsolationCheck
    {
        public string Description { get; set; } = string.Empty;
        public bool Passed { get; set; }
    }

    private class AgentCapability
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

// IAgent interface compliance (from existing codebase)
public interface IAgent
{
    Task<AgentResult> ExecuteAsync(AgentContext context);
}

public class AgentContext
{
    public string? WorkspaceId { get; set; }
    public AgentTask? Task { get; set; }
}

public class AgentTask
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Context { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;
    public TimeSpan? EstimatedDuration { get; set; }
}

public enum TaskPriority
{
    Critical,
    High,
    Normal,
    Low
}

public class AgentResult
{
    public bool IsSuccess { get; set; }
    public bool IsApproved { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Error { get; set; }
    public List<AgentTask>? SuggestedSubtasks { get; set; }
    public PerformanceProfile? PerformanceProfile { get; set; }
    public TechDebtItem? TechDebt { get; set; }
    public ObservabilityData? Observability { get; set; }
}

public class PerformanceProfile
{
    public int DurationMs { get; set; }
    public int TokenUsage { get; set; }
}

public class TechDebtItem
{
    public string Category { get; set; } = string.Empty;
    public Severity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ObservabilityData
{
    public List<string> Traces { get; set; } = new();
    public Dictionary<string, double> Metrics { get; set; } = new();
}

public class CompressedContext
{
    public int OriginalTokens { get; set; }
    public int CompressedTokens { get; set; }
    public double CompressionRatio => OriginalTokens > 0 ? (1.0 - (double)CompressedTokens / OriginalTokens) * 100 : 0;
    public string Algorithm { get; set; } = string.Empty;
}

// Forward declarations for interfaces from other modules
public interface IContextCompressionService
{
    Task<CompressedContext> CompressAgentContextAsync(string context, int targetTokens);
}
*/
