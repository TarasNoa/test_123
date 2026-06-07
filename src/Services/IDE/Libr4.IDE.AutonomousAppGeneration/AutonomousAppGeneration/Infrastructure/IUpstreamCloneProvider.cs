namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

public interface IUpstreamCloneProvider
{
    /// <summary>Shallow-clones <paramref name="cloneUrl"/> into a temp directory. Caller must dispose the handle.</summary>
    Task<UpstreamCloneHandle?> TryShallowCloneAsync(string cloneUrl, CancellationToken ct = default);
}

public sealed class UpstreamCloneHandle : IDisposable
{
    public UpstreamCloneHandle(string workspaceRoot, string cloneUrl, bool ownsPath = true)
    {
        WorkspaceRoot = workspaceRoot;
        CloneUrl = cloneUrl;
        OwnsPath = ownsPath;
    }

    public string WorkspaceRoot { get; }
    public string CloneUrl { get; }
    public bool OwnsPath { get; }

    public void Dispose()
    {
        if (!OwnsPath)
            return;

        try
        {
            var parent = Directory.GetParent(WorkspaceRoot)?.FullName;
            if (parent is not null && Directory.Exists(parent))
                Directory.Delete(parent, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
