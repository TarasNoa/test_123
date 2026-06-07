namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public interface IRunExportService
{
    Task<RunExportResult?> ExportAsync(Guid runId, CancellationToken ct = default);

    Task<(string Path, string FileName)?> TryResolveDownloadAsync(
        Guid runId,
        string exportId,
        CancellationToken ct = default);

    int PruneExpiredExports();
}
