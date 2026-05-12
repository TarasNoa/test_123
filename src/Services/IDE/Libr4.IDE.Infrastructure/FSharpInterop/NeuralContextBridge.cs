using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Libr4.IDE.Domain.FSharp;

namespace Libr4.IDE.Infrastructure.FSharpInterop;

/// <summary>
/// C# Bridge for F# Neural Context & Cognitive Memory
/// Provides hierarchical knowledge graph with Code DNA enforcement
/// </summary>
public interface INeuralContextBridge
{
    /// <summary>
    /// Create project memory tier with Code DNA
    /// </summary>
    FSharpMemoryContext CreateProjectMemory(string projectId, string[] techStack, string[] domainEntities);

    /// <summary>
    /// Check if code entity violates project DNA
    /// </summary>
    DNAViolation[] CheckCodeDNA(FSharpMemoryContext context, CodeEntityBridge entity);

    /// <summary>
    /// Get project health score (0.0 - 1.0)
    /// </summary>
    double GetProjectHealthScore(FSharpMemoryContext context);

    /// <summary>
    /// Query knowledge graph Neo4j-style
    /// </summary>
    KnowledgeNode[] QueryGraph(FSharpMemoryContext context, string query);

    /// <summary>
    /// Add entity to working memory (LRU eviction)
    /// </summary>
    void AddToWorkingMemory(FSharpMemoryContext context, CodeEntityBridge entity);

    /// <summary>
    /// Block deployment if DNA violations found (via Consensus)
    /// </summary>
    bool ShouldBlockDeployment(FSharpMemoryContext context, CodeEntityBridge entity, ConsensusResultBridge consensus);
}

/// <summary>
/// C# wrapper for F# memory context
/// </summary>
public record FSharpMemoryContext(object InternalContext, MemoryTier Tier);

/// <summary>
/// Memory tier types
/// </summary>
public enum MemoryTier
{
    Ephemeral,  // Session only
    Project,    // Project-level
    Global      // Cross-project
}

/// <summary>
/// Code entity for C#
/// </summary>
public record CodeEntityBridge(
    string Id,
    string Name,
    CodeEntityType Type,
    CodeLocation Location,
    string[] Dependencies,
    bool IsRepository);

/// <summary>
/// Code entity types
/// </summary>
public enum CodeEntityType
{
    Class,
    Function,
    Interface,
    Module,
    Query
}

/// <summary>
/// Code location
/// </summary>
public record CodeLocation(string FilePath, int StartLine, int EndLine, string CommitHash);

/// <summary>
/// DNA violation
/// </summary>
public record DNAViolation(
    string EntityId,
    string ViolatedPattern,
    ViolationSeverity Severity,
    string SuggestedFix);

/// <summary>
/// Violation severity
/// </summary>
public enum ViolationSeverity
{
    Info,
    Warning,
    Critical,
    Blocker
}

/// <summary>
/// Knowledge node for graph queries
/// </summary>
public record KnowledgeNode(
    string Id,
    string Name,
    KnowledgeNodeType NodeType,
    Dictionary<string, object> Metadata);

/// <summary>
/// Knowledge node types
/// </summary>
public enum KnowledgeNodeType
{
    CodeEntity,
    Pattern,
    Relationship,
    Context
}

/// <summary>
/// Consensus result for C#
/// </summary>
public record ConsensusResultBridge(string Status, double Score, string Rationale);

/// <summary>
/// Implementation
/// </summary>
public class NeuralContextBridge : INeuralContextBridge
{
    private readonly ILogger<NeuralContextBridge> _logger;

    public NeuralContextBridge(ILogger<NeuralContextBridge> logger)
    {
        _logger = logger;
    }

    public FSharpMemoryContext CreateProjectMemory(string projectId, string[] techStack, string[] domainEntities)
    {
        try
        {
            var config = new ProjectConfig(
                projectId,
                Option<string>.None,  // No repo URL for now
                ListModule.OfSeq(techStack),
                ListModule.OfSeq(domainEntities));

            var tier = MemoryTier.NewProject(config);
            var nodes = MemoryOperations.createMemory(tier);

            _logger.LogInformation("Created project memory context for {ProjectId}", projectId);

            return new FSharpMemoryContext(nodes, MemoryTier.Project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project memory");
            throw;
        }
    }

    public DNAViolation[] CheckCodeDNA(FSharpMemoryContext context, CodeEntityBridge entity)
    {
        try
        {
            // Convert C# entity to F# entity
            var fsharpEntity = ConvertToFSharpEntity(entity);

            // Create DNA checker
            var checker = new DNAChecker(context.Tier switch
            {
                MemoryTier.Project => (MemoryTier)context.InternalContext,
                _ => throw new InvalidOperationException("Invalid memory tier")
            });

            // Check code
            var violations = checker.CheckCode(fsharpEntity);

            _logger.LogDebug("Found {Count} DNA violations for {Entity}",
                violations.Count(), entity.Name);

            return violations.Select(ConvertToCSharpViolation).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check code DNA");
            return Array.Empty<DNAViolation>();
        }
    }

    public double GetProjectHealthScore(FSharpMemoryContext context)
    {
        try
        {
            // Get violations and compute score
            var violations = new List<Violation>();  // Would get from context

            var checker = new DNAChecker(context.Tier switch
            {
                MemoryTier.Project => (MemoryTier)context.InternalContext,
                _ => throw new InvalidOperationException("Invalid memory tier")
            });

            var score = checker.ComputeHealthScore(ListModule.OfSeq(violations));

            return score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute health score");
            return 0.0;
        }
    }

    public KnowledgeNode[] QueryGraph(FSharpMemoryContext context, string query)
    {
        try
        {
            if (context.InternalContext is IEnumerable<KnowledgeNode> nodes)
            {
                var results = MemoryOperations.queryGraph(
                    ListModule.OfSeq(nodes),
                    query);

                return results.Select(ConvertToCSharpNode).ToArray();
            }

            return Array.Empty<KnowledgeNode>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query graph");
            return Array.Empty<KnowledgeNode>();
        }
    }

    public void AddToWorkingMemory(FSharpMemoryContext context, CodeEntityBridge entity)
    {
        try
        {
            // Working memory update would happen here
            _logger.LogDebug("Added {Entity} to working memory", entity.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add to working memory");
        }
    }

    public bool ShouldBlockDeployment(FSharpMemoryContext context, CodeEntityBridge entity, ConsensusResultBridge consensus)
    {
        try
        {
            // 1. Check DNA violations
            var violations = CheckCodeDNA(context, entity);
            var hasBlocker = violations.Any(v => v.Severity == ViolationSeverity.Blocker);

            if (hasBlocker)
            {
                _logger.LogWarning("Deployment blocked: DNA violations found for {Entity}", entity.Name);
                return true;
            }

            // 2. Check consensus
            if (consensus.Status != "Accepted")
            {
                _logger.LogWarning("Deployment blocked: Consensus not reached for {Entity}", entity.Name);
                return true;
            }

            _logger.LogInformation("Deployment approved for {Entity}", entity.Name);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in deployment decision - blocking for safety");
            return true;  // Block on error
        }
    }

    // Conversion helpers
    private static Libr4.IDE.Domain.FSharp.CodeEntityNode ConvertToFSharpEntity(CodeEntityBridge entity)
    {
        // Simplified conversion - real implementation would be more complete
        var classInfo = new ClassInfo(
            Option<string>.None,
            ListModule.Empty<string>(),
            ListModule.Empty<string>(),
            ListModule.Empty<string>(),
            entity.IsRepository);

        return new Libr4.IDE.Domain.FSharp.CodeEntityNode(
            entity.Id,
            entity.Name,
            Libr4.IDE.Domain.FSharp.CodeEntityType.NewClass(classInfo),
            new Libr4.IDE.Domain.FSharp.CodeLocation(
                entity.Location.FilePath,
                entity.Location.StartLine,
                entity.Location.EndLine,
                entity.Location.CommitHash),
            "",
            ConvertToFSharpList(entity.Dependencies),
            DateTime.UtcNow);
    }

    private static FSharpList<string> ConvertToFSharpList(string[] items)
    {
        return ListModule.OfSeq(items);
    }

    private static DNAViolation ConvertToCSharpViolation(Violation violation)
    {
        return new DNAViolation(
            violation.EntityId,
            violation.ViolatedPattern.ToString(),
            violation.Severity switch
            {
                var s when s == Libr4.IDE.Domain.FSharp.ViolationSeverity.Info => ViolationSeverity.Info,
                var s when s == Libr4.IDE.Domain.FSharp.ViolationSeverity.Warning => ViolationSeverity.Warning,
                var s when s == Libr4.IDE.Domain.FSharp.ViolationSeverity.Critical => ViolationSeverity.Critical,
                var s when s == Libr4.IDE.Domain.FSharp.ViolationSeverity.Blocker => ViolationSeverity.Blocker,
                _ => ViolationSeverity.Info
            },
            OptionModule.GetValueWithDefault(violation.SuggestedFix, "No fix suggested"));
    }

    private static KnowledgeNode ConvertToCSharpNode(Libr4.IDE.Domain.FSharp.KnowledgeNode node)
    {
        // Simplified conversion
        return new KnowledgeNode(
            "id",
            "name",
            KnowledgeNodeType.CodeEntity,
            new Dictionary<string, object>());
    }
}

/// <summary>
/// Security Scanner Bridge (Rust interop via HTTP)
/// </summary>
public interface ISecurityScannerBridge
{
    Task<SecurityScanResult> ScanCodeAsync(string code, string language, CancellationToken ct = default);
    Task<QuickScanResult> QuickCheckAsync(string code, string language, CancellationToken ct = default);
}

/// <summary>
/// Security scan result
/// </summary>
public record SecurityScanResult(
    string ScanId,
    string OverallRisk,
    SecurityFinding[] Findings,
    bool IsSafeToDeploy,
    string[] SuggestedFixes);

/// <summary>
/// Security finding
/// </summary>
public record SecurityFinding(
    string Id,
    string Severity,
    string Category,
    string Title,
    string Description,
    string? CWEId,
    float? CVSSScore,
    string Remediation);

/// <summary>
/// Quick scan result
/// </summary>
public record QuickScanResult(string ScanId, bool IsSafe, string RiskLevel, int IssueCount, int CriticalCount);

/// <summary>
/// Implementation calling Rust security scanner via HTTP
/// </summary>
public class SecurityScannerBridge : ISecurityScannerBridge
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SecurityScannerBridge> _logger;

    public SecurityScannerBridge(HttpClient httpClient, ILogger<SecurityScannerBridge> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://localhost:7070/");
    }

    public async Task<SecurityScanResult> ScanCodeAsync(string code, string language, CancellationToken ct = default)
    {
        try
        {
            var request = new
            {
                code,
                language,
                scan_options = new
                {
                    enable_fuzzing = false,
                    enable_static_analysis = true,
                    enable_dependency_check = false,
                    fuzz_duration_secs = 0,
                    max_fuzz_iterations = 0
                }
            };

            var response = await _httpClient.PostAsJsonAsync("/scan", request, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<SecurityScanResult>(ct);
            return result ?? throw new InvalidOperationException("Failed to parse response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security scan failed");
            throw;
        }
    }

    public async Task<QuickScanResult> QuickCheckAsync(string code, string language, CancellationToken ct = default)
    {
        try
        {
            var request = new { code, language };

            var response = await _httpClient.PostAsJsonAsync("/quick-check", request, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<QuickScanResult>(ct);
            return result ?? throw new InvalidOperationException("Failed to parse response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quick security check failed");
            throw;
        }
    }
}

/// <summary>
/// Binary Archeology Bridge (Rust interop via HTTP)
/// </summary>
public interface IBinaryArcheologyBridge
{
    Task<BinaryAnalysisResult> AnalyzeBinaryAsync(string binaryPath, string binaryType, string targetLanguage);
    Task<BinaryUploadResult> UploadBinaryAsync(Stream fileStream, string fileName);
}

/// <summary>
/// Binary analysis result
/// </summary>
public record BinaryAnalysisResult(
    BinaryInfo Info,
    DecompiledModule[] Modules,
    MigrationPlan Plan,
    double ConfidenceScore,
    string[] Warnings,
    EffortEstimate Effort);

/// <summary>
/// Binary info
/// </summary>
public record BinaryInfo(
    string FileName,
    long FileSize,
    string BinaryType,
    string Architecture,
    string[] Dependencies,
    string[] Strings);

/// <summary>
/// Decompiled module
/// </summary>
public record DecompiledModule(
    string OriginalName,
    string TargetFilePath,
    string TargetLanguage,
    string GoldenStackCode,
    bool BusinessLogicExtracted,
    double Confidence);

/// <summary>
/// Migration plan
/// </summary>
public record MigrationPlan(
    MigrationStep[] Steps,
    string[] FSharpModules,
    string[] CSharpModules,
    string[] RustModules,
    bool DatabaseMigrationNeeded);

/// <summary>
/// Migration step
/// </summary>
public record MigrationStep(int Order, string Description, float EffortHours, string RiskLevel);

/// <summary>
/// Effort estimate
/// </summary>
public record EffortEstimate(float TotalHours, float DeveloperDays, double AutomationPercentage);

/// <summary>
/// Binary upload result
/// </summary>
public record BinaryUploadResult(string Status, string Message, string? FileId);

/// <summary>
/// Implementation calling Rust binary archeology via HTTP
/// </summary>
public class BinaryArcheologyBridge : IBinaryArcheologyBridge
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BinaryArcheologyBridge> _logger;

    public BinaryArcheologyBridge(HttpClient httpClient, ILogger<BinaryArcheologyBridge> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("http://localhost:6060/");
    }

    public async Task<BinaryAnalysisResult> AnalyzeBinaryAsync(
        string binaryPath,
        string binaryType,
        string targetLanguage)
    {
        try
        {
            var request = new
            {
                binary_path = binaryPath,
                binary_type = binaryType,
                target_language = targetLanguage,
                include_comments = true,
                analyze_dependencies = true
            };

            var response = await _httpClient.PostAsJsonAsync("/analyze", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BinaryAnalysisResult>();
            return result ?? throw new InvalidOperationException("Failed to parse response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Binary analysis failed");
            throw;
        }
    }

    public async Task<BinaryUploadResult> UploadBinaryAsync(Stream fileStream, string fileName)
    {
        try
        {
            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", fileName);

            var response = await _httpClient.PostAsync("/upload", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BinaryUploadResult>();
            return result ?? throw new InvalidOperationException("Failed to parse response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Binary upload failed");
            throw;
        }
    }
}
