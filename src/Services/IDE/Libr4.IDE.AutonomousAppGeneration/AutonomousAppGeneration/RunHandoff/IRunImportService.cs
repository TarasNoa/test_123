namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public interface IRunImportService
{
    Task<RunImportResult> ImportBundleAsync(string bundlePath, CancellationToken ct = default);

    Task<RunImportResult> ImportBundleStreamAsync(
        Stream bundleStream,
        string suggestedFileName,
        CancellationToken ct = default);
}
