using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public sealed class FileSystemVerifyEvidenceStore : IVerifyEvidenceStore
{
    private const string ApiRoutePrefix = "/api/ide/app-generation";

    private static readonly Dictionary<string, VerifyEvidenceKind> KnownFiles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["app.log"] = VerifyEvidenceKind.AppLog,
            ["readiness.json"] = VerifyEvidenceKind.Readiness,
            ["screenshot-final.png"] = VerifyEvidenceKind.Screenshot,
            ["smoke.webm"] = VerifyEvidenceKind.SmokeVideo,
            ["dom-snapshot.md"] = VerifyEvidenceKind.DomSnapshot,
            ["console-errors.json"] = VerifyEvidenceKind.ConsoleErrors,
            ["verify-report.json"] = VerifyEvidenceKind.VerifyReport,
            ["manifest.json"] = VerifyEvidenceKind.Manifest,
            ["verify-failure-evidence.json"] = VerifyEvidenceKind.FailureEvidence
        };

    private readonly VerifySubagentOptions _options;
    private readonly ILogger<FileSystemVerifyEvidenceStore> _logger;

    public FileSystemVerifyEvidenceStore(
        IOptions<VerifySubagentOptions> options,
        ILogger<FileSystemVerifyEvidenceStore> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string GetEvidenceDirectory(Guid runId) =>
        Path.Combine(_options.EvidenceRoot, runId.ToString("D"), "verify");

    public VerifyEvidenceBundle List(Guid runId)
    {
        var directory = GetEvidenceDirectory(runId);
        if (!Directory.Exists(directory))
        {
            return new VerifyEvidenceBundle(
                runId,
                directory,
                DirectoryExists: false,
                ThumbnailUrl: null,
                Artifacts: Array.Empty<VerifyEvidenceArtifact>());
        }

        var artifacts = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
            .Select(path => MapArtifact(runId, directory, path))
            .OrderBy(a => a.Kind)
            .ThenBy(a => a.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var thumbnail = artifacts.FirstOrDefault(a => a.Kind == VerifyEvidenceKind.Screenshot)?.ThumbnailUrl;

        return new VerifyEvidenceBundle(
            runId,
            directory,
            DirectoryExists: true,
            ThumbnailUrl: thumbnail,
            Artifacts: artifacts);
    }

    public VerifyEvidenceArtifact? TryGet(Guid runId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..", StringComparison.Ordinal))
            return null;

        var directory = GetEvidenceDirectory(runId);
        var absolute = Path.GetFullPath(Path.Combine(directory, fileName));
        if (!absolute.StartsWith(Path.GetFullPath(directory), StringComparison.OrdinalIgnoreCase))
            return null;

        return File.Exists(absolute)
            ? MapArtifact(runId, directory, absolute)
            : null;
    }

    public async Task<VerifyEvidenceArtifact> PersistAsync(
        Guid runId,
        VerifyEvidenceKind kind,
        Stream content,
        string? fileName = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var directory = GetEvidenceDirectory(runId);
        Directory.CreateDirectory(directory);

        var targetName = fileName ?? DefaultFileName(kind);
        var absolute = Path.Combine(directory, targetName);
        await using var output = File.Create(absolute);
        await content.CopyToAsync(output, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "[VerifyEvidence] Persisted {Kind} for run {RunId} -> {Path}",
            kind,
            runId,
            absolute);

        return MapArtifact(runId, directory, absolute);
    }

    public async Task<VerifyEvidenceArtifact> PersistFromPathAsync(
        Guid runId,
        VerifyEvidenceKind kind,
        string sourcePath,
        CancellationToken ct = default)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException($"Verify evidence source not found: {sourcePath}", sourcePath);

        await using var input = File.OpenRead(sourcePath);
        return await PersistAsync(runId, kind, input, DefaultFileName(kind), ct).ConfigureAwait(false);
    }

    private VerifyEvidenceArtifact MapArtifact(Guid runId, string directory, string absolutePath)
    {
        var fileName = Path.GetFileName(absolutePath);
        var kind = ResolveKind(fileName);
        var info = new FileInfo(absolutePath);
        var downloadUrl = BuildDownloadUrl(runId, fileName);
        var thumbnailUrl = kind == VerifyEvidenceKind.Screenshot ? downloadUrl : null;

        return new VerifyEvidenceArtifact(
            kind,
            fileName,
            Path.GetRelativePath(_options.EvidenceRoot, absolutePath).Replace('\\', '/'),
            absolutePath,
            info.Length,
            info.LastWriteTimeUtc,
            ResolveContentType(fileName),
            downloadUrl,
            thumbnailUrl);
    }

    private static VerifyEvidenceKind ResolveKind(string fileName)
    {
        if (KnownFiles.TryGetValue(fileName, out var kind))
            return kind;

        if (fileName.StartsWith("readiness-", StringComparison.OrdinalIgnoreCase)
            && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return VerifyEvidenceKind.Readiness;

        return VerifyEvidenceKind.Other;
    }

    private static string DefaultFileName(VerifyEvidenceKind kind) => kind switch
    {
        VerifyEvidenceKind.AppLog => "app.log",
        VerifyEvidenceKind.Readiness => "readiness.json",
        VerifyEvidenceKind.Screenshot => "screenshot-final.png",
        VerifyEvidenceKind.SmokeVideo => "smoke.webm",
        VerifyEvidenceKind.DomSnapshot => "dom-snapshot.md",
        VerifyEvidenceKind.ConsoleErrors => "console-errors.json",
        VerifyEvidenceKind.VerifyReport => "verify-report.json",
        VerifyEvidenceKind.Manifest => "manifest.json",
        VerifyEvidenceKind.FailureEvidence => "verify-failure-evidence.json",
        _ => $"artifact-{Guid.NewGuid():N}"
    };

    private static string BuildDownloadUrl(Guid runId, string fileName) =>
        $"{ApiRoutePrefix}/{runId:D}/verify/artifacts/{Uri.EscapeDataString(fileName)}";

    private static string? ResolveContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".json" => "application/json",
            ".png" => "image/png",
            ".webm" => "video/webm",
            ".md" => "text/markdown",
            ".log" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
