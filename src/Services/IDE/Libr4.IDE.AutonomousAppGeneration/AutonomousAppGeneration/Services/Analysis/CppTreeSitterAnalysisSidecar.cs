using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Analysis;

/// <summary>
/// Wave 6.1: in-process C++ tree-sitter analysis (implements <see cref="IRustAnalysisSidecar"/> contract).
/// Gracefully unavailable when <c>libr4_tree_sitter</c> native library is missing.
/// </summary>
public sealed class CppTreeSitterAnalysisSidecar : IRustAnalysisSidecar
{
    private readonly ILogger<CppTreeSitterAnalysisSidecar> _logger;

    public CppTreeSitterAnalysisSidecar(ILogger<CppTreeSitterAnalysisSidecar> logger) =>
        _logger = logger;

    public Task<SidecarAnalysisResult> AnalyzeAsync(
        IReadOnlyList<GeneratedFile> files,
        SidecarAnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        options ??= new SidecarAnalysisOptions();

        if (!CppTreeSitterBridge.IsAvailable)
        {
            return Task.FromResult(new SidecarAnalysisResult(
                Guid.NewGuid().ToString("n"),
                [],
                Error: "cpp_tree_sitter_unavailable"));
        }

        var results = new List<FileAnalysisResult>(files.Count);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!CppTreeSitterBridge.TryAnalyzeFile(file.RelativePath, file.Content, _logger, out var analyzed)
                || analyzed is null)
            {
                results.Add(new FileAnalysisResult(file.RelativePath));
                continue;
            }

            results.Add(FilterOptions(analyzed, options));
        }

        return Task.FromResult(new SidecarAnalysisResult(
            Guid.NewGuid().ToString("n"),
            results));
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CppTreeSitterBridge.IsAvailable);

    private static FileAnalysisResult FilterOptions(FileAnalysisResult result, SidecarAnalysisOptions options) =>
        new(
            result.Path,
            Complexity: options.ComputeComplexity ? result.Complexity : null,
            Placeholders: options.DetectPlaceholders ? result.Placeholders : [],
            SecurityIssues: options.DetectSecurityIssues ? result.SecurityIssues : [],
            TestQuality: options.AnalyzeTestQuality ? result.TestQuality : null);
}
