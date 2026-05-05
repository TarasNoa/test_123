using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Service for generating diagnostics bundles for debugging.
/// </summary>
public interface IDiagnosticsBundleService
{
    /// <summary>
    /// Generate a diagnostics bundle for a given orchestrator run.
    /// </summary>
    Task<DiagnosticsBundleDto?> GenerateBundleAsync(Guid orchestratorId, CancellationToken ct = default);
}
