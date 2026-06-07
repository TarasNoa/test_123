namespace Libr4.IDE.Application.AutonomousAppGeneration.FastContext;

public interface ICodebaseIndex
{
    Task IndexAsync(string workspaceRoot, Guid? runId = null, CancellationToken ct = default);

    Task<IReadOnlyList<CodebaseSearchHit>> SearchAsync(
        string workspaceRoot,
        string query,
        CodebaseSearchOptions? options = null,
        CancellationToken ct = default);

    Task<CodebaseSymbolContext?> GetSymbolAsync(
        string workspaceRoot,
        string symbol,
        string? pathHint = null,
        CancellationToken ct = default);

    Task InvalidateAsync(string workspaceRoot, CancellationToken ct = default);
}

public sealed record CodebaseSearchOptions(
    int Limit = 12,
    bool IncludeTests = false,
    IReadOnlyList<string>? Languages = null);

public sealed record CodebaseSearchHit(
    string Path,
    int StartLine,
    int EndLine,
    double Score,
    string Snippet,
    string MatchKind);

public sealed record CodebaseSymbolContext(
    string Symbol,
    string Path,
    int StartLine,
    int EndLine,
    string Snippet,
    IReadOnlyList<string> RelatedPaths);

public sealed record CodebaseIndexManifest(
    string WorkspaceRoot,
    string WorkspaceHash,
    DateTime IndexedAtUtc,
    int FileCount,
    IReadOnlyList<CodebaseIndexedFile> Files);

public sealed record CodebaseIndexedFile(string RelativePath, string ContentHash, long SizeBytes);
