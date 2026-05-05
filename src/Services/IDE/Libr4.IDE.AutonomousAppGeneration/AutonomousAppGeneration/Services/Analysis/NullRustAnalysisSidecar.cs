using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Analysis;

/// <summary>
/// P2-6 placeholder implementation of <see cref="IRustAnalysisSidecar"/>. Used when
/// the real Rust sidecar is not deployed (development, CI without native binaries,
/// single-host lightweight mode). Returns empty per-file results so downstream
/// consumers (ReviewGate2 semantic checks, deterministic fallback injection) can
/// continue without special-casing a missing sidecar.
/// </summary>
public sealed class NullRustAnalysisSidecar : IRustAnalysisSidecar
{
    private readonly ILogger<NullRustAnalysisSidecar>? _logger;

    public NullRustAnalysisSidecar(ILogger<NullRustAnalysisSidecar>? logger = null)
    {
        _logger = logger;
    }

    public Task<SidecarAnalysisResult> AnalyzeAsync(
        IReadOnlyList<GeneratedFile> files,
        SidecarAnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug(
            "NullRustAnalysisSidecar: no-op analysis for {FileCount} file(s); sidecar not deployed.",
            files?.Count ?? 0);

        var results = (files ?? Array.Empty<GeneratedFile>())
            .Select(f => new FileAnalysisResult(
                Path: f.RelativePath,
                Complexity: null,
                Placeholders: Array.Empty<PlaceholderFinding>(),
                SecurityIssues: Array.Empty<SecurityFinding>(),
                TestQuality: null))
            .ToList();

        return Task.FromResult(new SidecarAnalysisResult(
            RequestId: Guid.NewGuid().ToString("n"),
            Results: results,
            Error: null));
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false); // healthy=false tells callers the real sidecar is absent.
}
