using System.Collections.Concurrent;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Infrastructure;

public sealed class GenerationWorkspaceStore
{
    private sealed record Entry(Guid Id, string HostPath, DateTime CreatedUtc);

    private readonly ConcurrentDictionary<Guid, Entry> _entries = new();
    private readonly ILogger<GenerationWorkspaceStore> _logger;

    public GenerationWorkspaceStore(ILogger<GenerationWorkspaceStore> logger) => _logger = logger;

    public Guid Create(IReadOnlyList<GeneratedFile> files)
    {
        var id = Guid.NewGuid();
        var root = Path.Combine(
            Path.GetTempPath(),
            "libr4-agent-gen",
            id.ToString("N"));
        Directory.CreateDirectory(root);
        MaterializeFiles(root, files);
        _entries[id] = new Entry(id, root, DateTime.UtcNow);
        _logger.LogDebug("Created generation workspace {Id} at {Path} ({Count} files)", id, root, files.Count);
        return id;
    }

    public bool TryGetHostPath(Guid workspaceId, out string hostPath)
    {
        if (_entries.TryGetValue(workspaceId, out var entry))
        {
            hostPath = entry.HostPath;
            return true;
        }

        hostPath = string.Empty;
        return false;
    }

    public void SyncFromFiles(Guid workspaceId, IReadOnlyList<GeneratedFile> files)
    {
        if (!_entries.TryGetValue(workspaceId, out var entry))
            return;

        MaterializeFiles(entry.HostPath, files);
    }

    public void Dispose(Guid workspaceId)
    {
        if (!_entries.TryRemove(workspaceId, out var entry))
            return;

        try
        {
            if (Directory.Exists(entry.HostPath))
                Directory.Delete(entry.HostPath, recursive: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete generation workspace {Id}", workspaceId);
        }
    }

    private static void MaterializeFiles(string root, IReadOnlyList<GeneratedFile> files)
    {
        foreach (var file in files)
        {
            var repaired = StackArtifactCompleteness.RepairGeneratedFile(file);
            if (repaired is null
                || !StackArtifactCompleteness.IsPlausibleFilePath(repaired.RelativePath))
                continue;

            var safe = repaired.RelativePath.Replace('/', Path.DirectorySeparatorChar);
            var abs = Path.Combine(root, safe);
            var dir = Path.GetDirectoryName(abs);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(abs, repaired.Content ?? string.Empty);
        }
    }
}
