using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using MediatR;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Handlers;

public sealed class ExportDiagnosticsPackageQueryHandler
    : IRequestHandler<ExportDiagnosticsPackageQuery, DiagnosticsPackageExportDto?>
{
    private readonly IDiagnosticsBundleService _diagnostics;
    private readonly DiagnosticsExportOptions _options;
    private readonly string _exportRoot;

    public ExportDiagnosticsPackageQueryHandler(
        IDiagnosticsBundleService diagnostics,
        IOptions<DiagnosticsExportOptions> options)
    {
        _diagnostics = diagnostics;
        _options = options.Value;
        _exportRoot = string.IsNullOrWhiteSpace(_options.ExportRootPath)
            ? Path.Combine(Path.GetTempPath(), "libr4-autogen-diagnostics-exports")
            : _options.ExportRootPath;
        Directory.CreateDirectory(_exportRoot);
    }

    public async Task<DiagnosticsPackageExportDto?> Handle(ExportDiagnosticsPackageQuery request, CancellationToken ct)
    {
        var bundle = await _diagnostics.GenerateBundleAsync(request.OrchestratorId, ct);
        if (bundle is null)
            return null;

        var generatedAt = DateTime.UtcNow;
        var exportId = $"diagnostics-{bundle.RunId:N}-{generatedAt:yyyyMMddHHmmss}";
        var zipPath = Path.Combine(_exportRoot, $"{exportId}.zip");

        var json = JsonSerializer.Serialize(bundle, new JsonSerializerOptions { WriteIndented = true });
        var hash = ComputeSha256(json);

        await using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("diagnostics-bundle.json", CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            var bytes = Encoding.UTF8.GetBytes(json);
            await entryStream.WriteAsync(bytes, ct);
        }

        CleanupOldArtifacts();

        return new DiagnosticsPackageExportDto(
            RunId: bundle.RunId,
            ExportId: exportId,
            ContentSha256: hash,
            ArtifactPath: zipPath,
            GeneratedAtUtc: generatedAt);
    }

    private void CleanupOldArtifacts()
    {
        if (!Directory.Exists(_exportRoot))
            return;

        var retention = Math.Clamp(_options.RetentionHours, 1, 24 * 30);
        var maxArtifacts = Math.Clamp(_options.MaxArtifacts, 10, 5000);
        var cutoff = DateTime.UtcNow.AddHours(-retention);

        var files = new DirectoryInfo(_exportRoot)
            .GetFiles("diagnostics-*.zip", SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToList();

        foreach (var old in files.Where(f => f.LastWriteTimeUtc < cutoff))
            SafeDelete(old);

        var remaining = new DirectoryInfo(_exportRoot)
            .GetFiles("diagnostics-*.zip", SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToList();

        foreach (var extra in remaining.Skip(maxArtifacts))
            SafeDelete(extra);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void SafeDelete(FileInfo file)
    {
        try { file.Delete(); }
        catch (Exception ex)
        {
            // Log failure but don't fail the entire operation for cleanup issues
            System.Diagnostics.Debug.WriteLine($"Failed to delete {file.FullName}: {ex.Message}");
        }
    }
}
