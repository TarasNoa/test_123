using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using MediatR;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Handlers;

public sealed class GetBenchmarkDashboardExportQueryHandler
    : IRequestHandler<GetBenchmarkDashboardExportQuery, BenchmarkDashboardExportDto>
{
    private readonly IMediator _mediator;
    private readonly string _exportRoot;
    private readonly BenchmarkExportOptions _options;

    public GetBenchmarkDashboardExportQueryHandler(
        IMediator mediator,
        IOptions<BenchmarkExportOptions> options)
    {
        _mediator = mediator;
        _options = options.Value;
        _exportRoot = string.IsNullOrWhiteSpace(_options.ExportRootPath)
            ? Path.Combine(Path.GetTempPath(), "libr4-autogen-benchmark-exports")
            : _options.ExportRootPath;
        Directory.CreateDirectory(_exportRoot);
    }

    public async Task<BenchmarkDashboardExportDto> Handle(GetBenchmarkDashboardExportQuery request, CancellationToken ct)
    {
        var dashboard = await _mediator.Send(new GetBenchmarkDashboardQuery(request.Limit), ct);
        var generatedAt = DateTime.UtcNow;
        var exportId = $"benchmark-{generatedAt:yyyyMMddHHmmss}-{Guid.NewGuid():N}";

        var json = JsonSerializer.Serialize(dashboard, new JsonSerializerOptions { WriteIndented = true });
        var hash = ComputeSha256(json);
        var path = Path.Combine(_exportRoot, $"{exportId}.json");
        await File.WriteAllTextAsync(path, json, Encoding.UTF8, ct);
        CleanupOldArtifacts();

        return new BenchmarkDashboardExportDto(
            ExportId: exportId,
            ContentSha256: hash,
            ArtifactPath: path,
            GeneratedAtUtc: generatedAt,
            Dashboard: dashboard);
    }

    private void CleanupOldArtifacts()
    {
        if (!Directory.Exists(_exportRoot))
            return;

        var retention = Math.Clamp(_options.RetentionHours, 1, 24 * 30);
        var maxArtifacts = Math.Clamp(_options.MaxArtifacts, 10, 5000);
        var cutoff = DateTime.UtcNow.AddHours(-retention);

        var files = new DirectoryInfo(_exportRoot)
            .GetFiles("benchmark-*.json", SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToList();

        foreach (var old in files.Where(f => f.LastWriteTimeUtc < cutoff))
            SafeDelete(old);

        var remaining = new DirectoryInfo(_exportRoot)
            .GetFiles("benchmark-*.json", SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToList();

        foreach (var extra in remaining.Skip(maxArtifacts))
            SafeDelete(extra);
    }

    private static void SafeDelete(FileInfo file)
    {
        try { file.Delete(); } catch { /* best effort cleanup */ }
    }

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
