using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.Queries;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Handlers;

public sealed class ExportDesignArtifactQueryHandler
{
    private readonly IDesignArtifactService _designArtifactService;
    private readonly ILogger<ExportDesignArtifactQueryHandler> _logger;

    public ExportDesignArtifactQueryHandler(
        IDesignArtifactService designArtifactService,
        ILogger<ExportDesignArtifactQueryHandler> logger)
    {
        _designArtifactService = designArtifactService;
        _logger = logger;
    }

    public async Task<ExportDesignArtifactResult> Handle(ExportDesignArtifactQuery query, CancellationToken ct)
    {
        var artifact = await _designArtifactService.GetArtifactAsync(query.ArtifactId, ct);
        if (artifact == null)
            throw new InvalidOperationException($"Design artifact '{query.ArtifactId}' not found");

        var json = _designArtifactService.SerializeArtifact(artifact);
        var payloadBytes = Encoding.UTF8.GetByteCount(json);

        var exportPath = query.ExportPath ?? Path.Combine(
            Path.GetTempPath(),
            "libr4-design-artifacts",
            $"{artifact.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}.json");

        Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);
        await File.WriteAllTextAsync(exportPath, json, Encoding.UTF8, ct);

        _logger.LogInformation(
            "[DesignArtifact] Exported artifact {ArtifactId} to {Path} ({Bytes} bytes)",
            artifact.Id,
            exportPath,
            payloadBytes);

        return new ExportDesignArtifactResult(
            ArtifactId: artifact.Id,
            ExportPath: exportPath,
            ContentHash: artifact.ContentHash,
            PayloadBytes: payloadBytes,
            ExportedAtUtc: DateTime.UtcNow);
    }
}
