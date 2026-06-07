using System.Formats.Tar;
using System.IO.Compression;
using System.Text.Json;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

internal static class RunBundleArchive
{
    public static async Task ExtractTarGzAsync(string archivePath, string destinationDirectory, CancellationToken ct)
    {
        Directory.CreateDirectory(destinationDirectory);
        await using var fileStream = File.OpenRead(archivePath);
        await using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new TarReader(gzip);

        while (reader.GetNextEntry() is { } entry)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.DataStream is null)
                continue;

            var relative = entry.Name.Replace('\\', '/').TrimStart('/');
            if (string.IsNullOrWhiteSpace(relative))
                continue;

            var target = Path.Combine(destinationDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
            var targetDir = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            await using var output = File.Create(target);
            if (entry.DataStream is not null)
                await entry.DataStream.CopyToAsync(output, ct).ConfigureAwait(false);
        }
    }

    public static async Task<RunExportManifest?> ReadManifestAsync(string extractRoot, CancellationToken ct)
    {
        var path = Path.Combine(extractRoot, "run-manifest.json");
        if (!File.Exists(path))
            return null;

        var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<RunExportManifest>(json, JsonOptions());
    }

    public static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}
