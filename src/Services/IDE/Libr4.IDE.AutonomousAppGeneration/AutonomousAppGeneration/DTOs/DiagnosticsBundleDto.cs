namespace Libr4.IDE.Application.AutonomousAppGeneration.DTOs;

/// <summary>
/// Diagnostics bundle containing run snapshot for debugging.
/// </summary>
public sealed record DiagnosticsBundleDto(
    Guid RunId,
    string BundleId,
    DateTime GeneratedAtUtc,
    DiagnosticsManifestDto Manifest,
    DiagnosticsLogsDto Logs,
    DiagnosticsFilesDto Files);

/// <summary>
/// Manifest information for diagnostics.
/// </summary>
public sealed record DiagnosticsManifestDto(
    string Status,
    string? FailureReason,
    int IterationCount,
    int FileCount,
    int QualityGateCount,
    BenchmarkSummaryDto BenchmarkSummary,
    IReadOnlyList<McpLaneDiagnosticsDto> McpLaneDiagnostics,
    IReadOnlyList<McpLaneWatchdogSnapshotDto> McpLaneWatchdogSnapshot);

public sealed record McpLaneDiagnosticsDto(
    string Lane,
    int DegradedEvents,
    IReadOnlyList<string> TopBlockerCodes);

/// <summary>
/// Watchdog telemetry snapshot for MCP lane preflight status.
/// </summary>
public sealed record McpLaneWatchdogSnapshotDto(
    string ProfileKey,
    string Lane,
    DateTime LastCheckTimeUtc,
    string Status, // "available" or "degraded"
    string? BlockerCode,
    string? DiagnosticMessage,
    IReadOnlyList<McpLaneWatchdogHistoryEntryDto> History);

/// <summary>
/// Watchdog history entry for diagnostics output.
/// </summary>
public sealed record McpLaneWatchdogHistoryEntryDto(
    DateTime CheckTimeUtc,
    string Status,
    string? BlockerCode);

/// <summary>
/// Log information for diagnostics.
/// </summary>
public sealed record DiagnosticsLogsDto(
    string SystemLogs,
    string ApplicationLogs,
    string ErrorLogs);

/// <summary>
/// File information for diagnostics.
/// </summary>
public sealed record DiagnosticsFilesDto(
    IReadOnlyList<DiagnosticsFileEntryDto> Files);

/// <summary>
/// Individual file entry in diagnostics bundle.
/// </summary>
public sealed record DiagnosticsFileEntryDto(
    string RelativePath,
    string Language,
    int SizeBytes,
    string Content);

public sealed record DiagnosticsPackageExportDto(
    Guid RunId,
    string ExportId,
    string ContentSha256,
    string ArtifactPath,
    DateTime GeneratedAtUtc);
