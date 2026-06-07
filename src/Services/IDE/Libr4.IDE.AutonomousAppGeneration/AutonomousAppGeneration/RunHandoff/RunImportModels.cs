namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed class RunImportOptions
{
    public const string SectionName = "AutonomousAppGeneration:RunImport";

    public string IdempotencyRootPath { get; set; } = Path.Combine(".logs", "run-imports");

    public long MaxBundleBytes { get; set; } = 2L * 1024 * 1024 * 1024;
}

public sealed record RunImportResult(
    Guid RunId,
    Guid SourceRunId,
    string BundleSha256,
    int LastStepNumber,
    bool IdempotentReplay,
    DateTime ImportedAtUtc,
    string? ResumeHint);

public sealed record RunPromoteResult(
    Guid RunId,
    Guid SourceRunId,
    string ExportId,
    string BundleSha256,
    string Status,
    DateTime PromotedAtUtc);

public sealed record RunImportLineage(
    Guid SourceRunId,
    Guid ImportedRunId,
    string BundleSha256,
    int LastStepNumber,
    DateTime ImportedAtUtc);

public sealed class RunImportException : Exception
{
    public RunImportException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
