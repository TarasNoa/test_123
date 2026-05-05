# Rust Sidecar Interface Specification

## Purpose

The Rust sidecar provides CPU-bound semantic analysis capabilities for generated code via tree-sitter parsing. This enables:

- **Placeholder detection**: Identify TODO/FIXME/HACK comments and stub implementations
- **Complexity scoring**: Calculate cyclomatic complexity, nesting depth, and function size metrics
- **Test quality assessment**: Analyze test coverage patterns, assertion quality, and test completeness
- **Security scanning**: Basic AST-based security pattern detection (hardcoded secrets, SQL injection risks)

The sidecar runs as a separate process to leverage Rust's performance and tree-sitter ecosystem while keeping the .NET host lightweight.

## IPC Design

### Option A: Named Pipes (Windows) / Unix Domain Sockets (Linux)

**Pros:**
- Low latency for single-host deployments
- No network stack overhead
- Simple authentication via filesystem permissions

**Cons:**
- Not suitable for multi-host deployments
- Platform-specific setup

**Protocol:**
```
Request (JSON):
{
  "requestId": "guid",
  "files": [
    {
      "path": "src/Services/MyService.cs",
      "content": "base64_encoded_content",
      "language": "csharp"
    }
  ],
  "analysisType": "complexity" | "placeholder" | "security" | "all"
}

Response (JSON):
{
  "requestId": "guid",
  "results": [
    {
      "path": "src/Services/MyService.cs",
      "complexity": {
        "cyclomaticComplexity": 12,
        "nestingDepth": 4,
        "functionCount": 8,
        "linesOfCode": 245
      },
      "placeholders": [
        {
          "line": 42,
          "type": "TODO",
          "message": "Implement error handling"
        }
      ],
      "securityIssues": []
    }
  ],
  "error": null
}
```

### Option B: gRPC (Recommended for Production)

**Pros:**
- Language-agnostic
- Supports multi-host deployments
- Built-in streaming for large file sets
- Strong typing via Protobuf

**Cons:**
- Additional dependency
- Slightly higher latency than named pipes

**Protobuf Definition:**
```protobuf
syntax = "proto3";

package RustSidecar;

service AnalysisService {
  rpc AnalyzeFiles (AnalyzeRequest) returns (AnalyzeResponse);
  rpc StreamAnalyze (stream FileChunk) returns (stream AnalysisResult);
}

message AnalyzeRequest {
  string request_id = 1;
  repeated FileInfo files = 2;
  AnalysisOptions options = 3;
}

message FileInfo {
  string path = 1;
  bytes content = 2;
  string language = 3; // csharp, python, javascript, typescript, rust, go
}

message AnalysisOptions {
  bool compute_complexity = 1;
  bool detect_placeholders = 2;
  bool detect_security_issues = 3;
  bool analyze_test_quality = 4;
}

message AnalyzeResponse {
  string request_id = 1;
  repeated FileAnalysis results = 2;
  string error = 3;
}

message FileAnalysis {
  string path = 1;
  ComplexityMetrics complexity = 2;
  repeated Placeholder placeholders = 3;
  repeated SecurityIssue security_issues = 4;
  TestQualityMetrics test_quality = 5;
}

message ComplexityMetrics {
  int32 cyclomatic_complexity = 1;
  int32 nesting_depth = 2;
  int32 function_count = 3;
  int32 lines_of_code = 4;
}

message Placeholder {
  int32 line = 1;
  string type = 2; // TODO, FIXME, HACK, XXX
  string message = 3;
}

message SecurityIssue {
  int32 line = 1;
  string severity = 2; // low, medium, high, critical
  string rule_id = 3;
  string description = 4;
}

message TestQualityMetrics {
  int32 assertion_count = 1;
  int32 coverage_estimate = 2; // 0-100
  repeated string missing_scenarios = 3;
}
```

## .NET Interface

```csharp
namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Analysis;

/// <summary>
/// Interface for Rust sidecar analysis service.
/// Provides CPU-bound semantic analysis via tree-sitter parsing.
/// </summary>
public interface IRustAnalysisSidecar
{
    /// <summary>
    /// Analyzes a collection of generated files for complexity, placeholders, and security issues.
    /// </summary>
    /// <param name="files">Files to analyze with their content.</param>
    /// <param name="options">Analysis options (which metrics to compute).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analysis results per file.</returns>
    Task<SidecarAnalysisResult> AnalyzeAsync(
        IReadOnlyList<GeneratedFile> files,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the sidecar is available and responsive.
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Analysis result from Rust sidecar.
/// </summary>
public sealed class SidecarAnalysisResult
{
    public string RequestId { get; init; } = null!;
    public IReadOnlyList<FileAnalysisResult> Results { get; init; } = Array.Empty<FileAnalysisResult>();
    public string? Error { get; init; }
}

/// <summary>
/// Per-file analysis result.
/// </summary>
public sealed class FileAnalysisResult
{
    public string Path { get; init; } = null!;
    public ComplexityMetrics? Complexity { get; init; }
    public IReadOnlyList<Placeholder> Placeholders { get; init; } = Array.Empty<Placeholder>();
    public IReadOnlyList<SecurityIssue> SecurityIssues { get; init; } = Array.Empty<SecurityIssue>();
    public TestQualityMetrics? TestQuality { get; init; }
}

/// <summary>
/// Complexity metrics for a file.
/// </summary>
public sealed class ComplexityMetrics
{
    public int CyclomaticComplexity { get; init; }
    public int NestingDepth { get; init; }
    public int FunctionCount { get; init; }
    public int LinesOfCode { get; init; }
}

/// <summary>
/// Placeholder comment detection result.
/// </summary>
public sealed class Placeholder
{
    public int Line { get; init; }
    public string Type { get; init; } = null!; // TODO, FIXME, HACK, XXX
    public string Message { get; init; } = null!;
}

/// <summary>
/// Security issue detection result.
/// </summary>
public sealed class SecurityIssue
{
    public int Line { get; init; }
    public string Severity { get; init; } = null!; // low, medium, high, critical
    public string RuleId { get; init; } = null!;
    public string Description { get; init; } = null!;
}

/// <summary>
/// Test quality assessment result.
/// </summary>
public sealed class TestQualityMetrics
{
    public int AssertionCount { get; init; }
    public int CoverageEstimate { get; init; } // 0-100
    public IReadOnlyList<string> MissingScenarios { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Analysis options to control which metrics are computed.
/// </summary>
public sealed class AnalysisOptions
{
    public bool ComputeComplexity { get; init; } = true;
    public bool DetectPlaceholders { get; init; } = true;
    public bool DetectSecurityIssues { get; init; } = true;
    public bool AnalyzeTestQuality { get; init; } = false;
}
```

## Placeholder No-Op Implementation

```csharp
namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Analysis;

/// <summary>
/// No-op implementation of IRustAnalysisSidecar for environments where the Rust sidecar is not deployed.
/// Returns empty results without performing actual analysis.
/// </summary>
public sealed class NullRustAnalysisSidecar : IRustAnalysisSidecar
{
    private readonly ILogger<NullRustAnalysisSidecar> _logger;

    public NullRustAnalysisSidecar(ILogger<NullRustAnalysisSidecar> logger)
    {
        _logger = logger;
    }

    public Task<SidecarAnalysisResult> AnalyzeAsync(
        IReadOnlyList<GeneratedFile> files,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("NullRustAnalysisSidecar: Skipping analysis for {FileCount} files", files.Count);

        var results = files.Select(f => new FileAnalysisResult
        {
            Path = f.RelativePath,
            Complexity = null,
            Placeholders = Array.Empty<Placeholder>(),
            SecurityIssues = Array.Empty<SecurityIssue>(),
            TestQuality = null
        }).ToList();

        return Task.FromResult(new SidecarAnalysisResult
        {
            RequestId = Guid.NewGuid().ToString(),
            Results = results,
            Error = null
        });
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
```

## Integration Points

### 1. Quality Gate Integration

The sidecar analysis can be integrated into `ReviewGate2Service` to enhance semantic checks:

```csharp
// In ReviewGate2Service
private async Task<ArchitectureCheckResult> CheckComplexityAsync(
    IReadOnlyList<GeneratedFile> files,
    GenerationPlan plan,
    CancellationToken ct)
{
    var result = await _rustSidecar.AnalyzeAsync(files, new AnalysisOptions
    {
        ComputeComplexity = true,
        DetectPlaceholders = false,
        DetectSecurityIssues = false
    }, ct);

    var maxComplexity = result.Results.Max(r => r.Complexity?.CyclomaticComplexity ?? 0);
    
    if (maxComplexity > 20)
    {
        return Fail($"complexity_threshold_exceeded:max={maxComplexity}");
    }

    return Pass();
}
```

### 2. Deterministic Fallback Integration

Use placeholder detection to trigger deterministic fallback injection:

```csharp
// In LlmCodeGenerationService
var analysis = await _rustSidecar.AnalyzeAsync(files, new AnalysisOptions
{
    DetectPlaceholders = true
}, ct);

var hasPlaceholders = analysis.Results.Any(r => r.Placeholders.Any());
if (hasPlaceholders)
{
    files = BuildDeterministicFallbackFixes(files, plan);
}
```

## Deployment Considerations

### Single-Host (Development)
- Use named pipes / Unix domain sockets
- Sidecar runs as background service alongside host
- Configuration: `RUST_SIDECAR_PIPE_NAME=libr4-analysis`

### Multi-Host (Production)
- Use gRPC with TLS
- Sidecar deployed as separate service (Kubernetes Deployment)
- Load balancing via Kubernetes Service
- Configuration: `RUST_SIDECAR_GRPC_ENDPOINT=rust-sidecar:50051`

### Fallback Behavior
- If sidecar unavailable (`IsHealthyAsync` returns false), fall back to:
  1. NullRustAnalysisSidecar (no-op) for graceful degradation
  2. Legacy substring-based checks as temporary measure
  3. Log warning for monitoring

## Future Enhancements

- **Incremental analysis**: Only analyze changed files between iterations
- **Caching**: Cache analysis results per file hash
- **Custom rules**: Allow user-defined tree-sitter queries
- **Multi-language support**: Expand beyond C#/Python/Node to Go/Rust/Java
- **Performance profiling**: Identify hot functions for optimization suggestions
