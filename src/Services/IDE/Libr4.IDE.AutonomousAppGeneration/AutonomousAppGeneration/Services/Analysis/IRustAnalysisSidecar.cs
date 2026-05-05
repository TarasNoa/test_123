using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Analysis;

/// <summary>
/// P2-6 of audit roadmap. Abstraction for the Rust sidecar that performs CPU-bound
/// semantic analysis via tree-sitter (placeholder detection, complexity scoring,
/// security scanning, test quality assessment).
///
/// The .NET host keeps this interface stable; the sidecar transport (named pipes,
/// gRPC) and the Rust implementation are deployment concerns that vary by environment.
/// See <c>docs/RUST_SIDECAR_SPEC.md</c> for the full design, IPC protocol and
/// protobuf schema.
/// </summary>
public interface IRustAnalysisSidecar
{
    /// <summary>
    /// Analyze the supplied generated files for complexity, placeholders, security issues
    /// and test quality. Implementations may short-circuit to a no-op when the sidecar
    /// is unavailable — callers must treat the result's <see cref="SidecarAnalysisResult.Error"/>
    /// as a soft signal and never as a pipeline-fatal condition.
    /// </summary>
    Task<SidecarAnalysisResult> AnalyzeAsync(
        IReadOnlyList<GeneratedFile> files,
        SidecarAnalysisOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the sidecar process is reachable and responsive. Callers should
    /// check this before opting into expensive analyses or flip to a fallback path.
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Top-level analysis result returned by <see cref="IRustAnalysisSidecar.AnalyzeAsync"/>.
/// </summary>
public sealed record SidecarAnalysisResult(
    string RequestId,
    IReadOnlyList<FileAnalysisResult> Results,
    string? Error = null);

/// <summary>
/// Per-file analysis bundle. Any metric may be null when the corresponding option was
/// disabled or when the sidecar could not parse the file (e.g. unsupported language).
/// </summary>
public sealed record FileAnalysisResult(
    string Path,
    ComplexityMetrics? Complexity = null,
    IReadOnlyList<PlaceholderFinding>? Placeholders = null,
    IReadOnlyList<SecurityFinding>? SecurityIssues = null,
    TestQualityMetrics? TestQuality = null);

public sealed record ComplexityMetrics(
    int CyclomaticComplexity,
    int NestingDepth,
    int FunctionCount,
    int LinesOfCode);

public sealed record PlaceholderFinding(
    int Line,
    string Type, // TODO, FIXME, HACK, XXX, STUB
    string Message);

public sealed record SecurityFinding(
    int Line,
    string Severity, // low, medium, high, critical
    string RuleId,
    string Description);

public sealed record TestQualityMetrics(
    int AssertionCount,
    int CoverageEstimate, // 0..100
    IReadOnlyList<string> MissingScenarios);

/// <summary>
/// Opt-in/out of individual analyses. Defaults: complexity on, placeholders on,
/// security on, test quality off (expensive; callers enable only when running review2).
/// </summary>
public sealed record SidecarAnalysisOptions(
    bool ComputeComplexity = true,
    bool DetectPlaceholders = true,
    bool DetectSecurityIssues = true,
    bool AnalyzeTestQuality = false);
