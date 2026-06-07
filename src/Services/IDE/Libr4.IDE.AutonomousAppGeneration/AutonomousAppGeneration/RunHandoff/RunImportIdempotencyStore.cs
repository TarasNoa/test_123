using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public interface IRunImportIdempotencyStore
{
    Task<RunImportLineage?> FindByBundleShaAsync(string bundleSha256, CancellationToken ct = default);

    Task SaveAsync(RunImportLineage lineage, CancellationToken ct = default);
}

public sealed class FileRunImportIdempotencyStore : IRunImportIdempotencyStore
{
    private static readonly JsonSerializerOptions JsonOptions = RunBundleArchive.JsonOptions();

    private readonly RunImportOptions _options;

    public FileRunImportIdempotencyStore(Microsoft.Extensions.Options.IOptions<RunImportOptions> options) =>
        _options = options.Value;

    public async Task<RunImportLineage?> FindByBundleShaAsync(string bundleSha256, CancellationToken ct = default)
    {
        var path = PathFor(bundleSha256);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<RunImportLineage>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync(RunImportLineage lineage, CancellationToken ct = default)
    {
        var root = Path.GetFullPath(_options.IdempotencyRootPath);
        Directory.CreateDirectory(root);
        var path = PathFor(lineage.BundleSha256);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(lineage, JsonOptions), ct).ConfigureAwait(false);
    }

    private string PathFor(string sha256) =>
        Path.Combine(Path.GetFullPath(_options.IdempotencyRootPath), $"{sha256}.json");
}
