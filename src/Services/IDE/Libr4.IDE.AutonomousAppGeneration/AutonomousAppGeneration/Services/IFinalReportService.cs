using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Trace linkage reference in final report.
/// </summary>
public sealed record TraceLinkageReference(
    string LinkageType,
    string Identifier,
    string Description);

/// <summary>
/// Final generation report with complete trace linkage.
/// </summary>
public sealed record FinalGenerationReport(
    string RunId,
    string ApplicationName,
    bool Success,
    int TotalIterations,
    IReadOnlyList<AgentTaskGraphEntry> TaskGraph,
    IReadOnlyList<string> ExecutedSkills,
    IReadOnlyList<string> McpCalls,
    IReadOnlyList<string> MemoryHits,
    string ReviewGateVerdict,
    IReadOnlyList<TraceLinkageReference> TraceLinkage,
    IReadOnlyList<string> Artifacts,
    DateTime GeneratedAtUtc);

/// <summary>
/// Serialization contract for report payload shape validation.
/// </summary>
public sealed record ReportSerializationContract(
    string Version,
    IReadOnlyList<string> RequiredFields,
    IReadOnlyList<string> OptionalFields,
    int MaxPayloadSizeBytes);

/// <summary>
/// Service for generating final reports with complete trace linkage and serialization validation.
/// </summary>
public interface IFinalReportService
{
    /// <summary>
    /// Generate final report with trace linkage from orchestrator state.
    /// </summary>
    FinalGenerationReport GenerateFinalReport(
        AppGenerationOrchestrator orchestrator,
        string reviewGateVerdict,
        IReadOnlyList<string> artifacts);

    /// <summary>
    /// Validate report payload shape against serialization contract.
    /// </summary>
    bool ValidateReportShape(
        FinalGenerationReport report,
        ReportSerializationContract contract);

    /// <summary>
    /// Get serialization contract for report version.
    /// </summary>
    ReportSerializationContract GetReportContract(string version);

    /// <summary>
    /// Serialize report to JSON with validation.
    /// </summary>
    string SerializeReport(
        FinalGenerationReport report,
        ReportSerializationContract contract);
}
