using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

/// <summary>
/// Default <see cref="IWorkspacePool"/> implementation.
///
/// Workspaces that ask for the same <c>runtimeImage</c> are grouped into the
/// same long-living <see cref="IRuntimeSession"/> — that session owns a top
/// level host directory; each workspace lives in its own subfolder inside it
/// (both on the host and, via the bind-mount, inside the guest).
///
/// When the last workspace of a session is released, the session itself is
/// disposed and its host directory is cleaned up.
/// </summary>
public sealed class VmWorkspacePool : IWorkspacePool, IAsyncDisposable
{
    private readonly IIsolatedRuntime _runtime;
    private readonly ILogger<VmWorkspacePool> _logger;
    private readonly string _rootPath;
    private readonly object _gate = new();

    // image -> session metadata
    private readonly Dictionary<string, SessionBucket> _sessions = new();
    // workspaceId -> session image (for reverse lookup on release)
    private readonly ConcurrentDictionary<Guid, string> _workspaceToImage = new();

    public VmWorkspacePool(IIsolatedRuntime runtime, ILogger<VmWorkspacePool> logger)
    {
        _runtime = runtime;
        _logger = logger;
        _rootPath = Path.Combine(Path.GetTempPath(), "libr4-shadow-pool");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<WorkspaceHandle> AcquireAsync(string runtimeImage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(runtimeImage)) throw new ArgumentException(nameof(runtimeImage));

        // Find or create the session for this image.
        SessionBucket? bucket;
        bool mustCreate;
        lock (_gate)
        {
            if (_sessions.TryGetValue(runtimeImage, out bucket))
            {
                mustCreate = false;
            }
            else
            {
                mustCreate = true;
                bucket = new SessionBucket
                {
                    HostRoot = Path.Combine(_rootPath, SanitizeImage(runtimeImage) + "-" + Guid.NewGuid().ToString("N")[..8]),
                };
                _sessions[runtimeImage] = bucket;
            }
        }

        if (mustCreate)
        {
            Directory.CreateDirectory(bucket!.HostRoot);
            try
            {
                bucket.Session = await _runtime.StartSessionAsync(runtimeImage, bucket.HostRoot, ct);
            }
            catch
            {
                lock (_gate) { _sessions.Remove(runtimeImage); }
                TryDelete(bucket.HostRoot);
                throw;
            }
        }

        // Create the workspace subfolder.
        var workspaceId = Guid.NewGuid();
        var folderName = workspaceId.ToString("N");
        var hostPath = Path.Combine(bucket!.HostRoot, folderName);
        Directory.CreateDirectory(hostPath);

        var guestPath = $"{bucket.Session!.GuestMountPath.TrimEnd('/')}/{folderName}";
        var handle = new WorkspaceHandle(workspaceId, hostPath, guestPath, bucket.Session);

        lock (_gate) { bucket.Workspaces.Add(workspaceId, handle); }
        _workspaceToImage[workspaceId] = runtimeImage;

        _logger.LogInformation(
            "Acquired workspace {Id} in session {Session} (image={Image})",
            workspaceId, bucket.Session.SessionId, runtimeImage);
        return handle;
    }

    public async Task ReleaseAsync(WorkspaceHandle handle, CancellationToken ct = default)
    {
        if (!_workspaceToImage.TryRemove(handle.WorkspaceId, out var image)) return;

        SessionBucket? bucket;
        bool disposeSession = false;
        lock (_gate)
        {
            if (!_sessions.TryGetValue(image, out bucket)) return;
            bucket.Workspaces.Remove(handle.WorkspaceId);
            if (bucket.Workspaces.Count == 0)
            {
                _sessions.Remove(image);
                disposeSession = true;
            }
        }

        TryDelete(handle.HostPath);

        if (disposeSession && bucket?.Session is { } session)
        {
            try { await session.DisposeAsync(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to dispose runtime session {Id}", session.SessionId); }
            TryDelete(bucket.HostRoot);
        }
    }

    public IReadOnlyList<WorkspaceHandle> ListActive()
    {
        lock (_gate)
        {
            return _sessions.Values
                .SelectMany(b => b.Workspaces.Values)
                .ToList();
        }
    }

    public async ValueTask DisposeAsync()
    {
        List<SessionBucket> buckets;
        lock (_gate)
        {
            buckets = _sessions.Values.ToList();
            _sessions.Clear();
        }
        foreach (var bucket in buckets)
        {
            if (bucket.Session is { } session)
            {
                try { await session.DisposeAsync(); }
                catch (Exception ex) { _logger.LogWarning(ex, "Error disposing session"); }
            }
            TryDelete(bucket.HostRoot);
        }
    }

    private void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to delete {Path}", path);
        }
    }

    private static string SanitizeImage(string image)
    {
        var chars = image.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
        return new string(chars);
    }

    private sealed class SessionBucket
    {
        public string HostRoot { get; init; } = string.Empty;
        public IRuntimeSession? Session { get; set; }
        public Dictionary<Guid, WorkspaceHandle> Workspaces { get; } = new();
    }
}
