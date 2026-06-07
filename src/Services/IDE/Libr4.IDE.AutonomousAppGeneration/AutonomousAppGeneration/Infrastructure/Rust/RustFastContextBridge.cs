using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Libr4.IDE.Application.AutonomousAppGeneration.FastContext;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Rust;

/// <summary>Rust native fast-context search (Wave 3.3). Falls back gracefully when cdylib is unavailable.</summary>
internal static class RustFastContextBridge
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static bool? _available;

    public static bool IsAvailable
    {
        get
        {
            if (_available.HasValue)
                return _available.Value;

            _available = RustNativeLibrary.TryLoad(
                () =>
                {
                    var rc = RustFastContextNative.fast_context_search_json("{}", out _);
                    return rc;
                },
                out _,
                out _);
            return _available.Value;
        }
    }

    public static bool TrySearch(
        string workspaceRoot,
        string query,
        CodebaseSearchOptions options,
        ILogger logger,
        out IReadOnlyList<CodebaseSearchHit> hits)
    {
        hits = Array.Empty<CodebaseSearchHit>();
        if (!IsAvailable)
            return false;

        try
        {
            var request = JsonSerializer.Serialize(new
            {
                workspaceRoot,
                query,
                maxMatches = 120,
                includeTests = options.IncludeTests,
                languages = options.Languages?.Count > 0 ? options.Languages : null
            }, JsonOptions);

            var rc = RustFastContextNative.fast_context_search_json(request, out var jsonPtr);
            if (rc != 0 || jsonPtr == IntPtr.Zero)
                return false;

            var json = Marshal.PtrToStringUTF8(jsonPtr) ?? "[]";
            RustFastContextNative.fast_context_free_string(jsonPtr);

            if (json.Contains("\"error\"", StringComparison.Ordinal))
            {
                logger.LogDebug("[RustFastContext] search error: {Json}", json);
                return false;
            }

            var dtoHits = JsonSerializer.Deserialize<RustSearchHitDto[]>(json, JsonOptions) ?? Array.Empty<RustSearchHitDto>();
            hits = dtoHits
                .Take(Math.Max(1, options.Limit))
                .Select(h => new CodebaseSearchHit(
                    h.Path,
                    h.StartLine,
                    h.EndLine,
                    h.Score,
                    h.Snippet,
                    h.MatchKind))
                .ToList();
            return hits.Count > 0;
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            _available = false;
            logger.LogDebug(ex, "[RustFastContext] native library unavailable");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[RustFastContext] search failed, using fallback");
            return false;
        }
    }

    public static bool TryBuildManifest(
        string workspaceRoot,
        ILogger logger,
        out CodebaseIndexManifest? manifest)
    {
        manifest = null;
        if (!IsAvailable)
            return false;

        try
        {
            var rc = RustFastContextNative.fast_context_build_manifest_json(workspaceRoot, out var jsonPtr);
            if (rc != 0 || jsonPtr == IntPtr.Zero)
                return false;

            var json = Marshal.PtrToStringUTF8(jsonPtr) ?? "{}";
            RustFastContextNative.fast_context_free_string(jsonPtr);

            var dto = JsonSerializer.Deserialize<RustManifestDto>(json, JsonOptions);
            if (dto is null)
                return false;

            manifest = new CodebaseIndexManifest(
                dto.WorkspaceRoot,
                dto.WorkspaceHash,
                DateTime.UtcNow,
                dto.FileCount,
                dto.Files.Select(f => new CodebaseIndexedFile(f.RelativePath, f.ContentHash, f.SizeBytes)).ToList());
            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[RustFastContext] manifest build failed, using fallback");
            return false;
        }
    }

    private sealed record RustSearchHitDto(
        string Path,
        int StartLine,
        int EndLine,
        double Score,
        string Snippet,
        string MatchKind);

    private sealed record RustManifestDto(
        string WorkspaceRoot,
        string WorkspaceHash,
        int FileCount,
        RustIndexedFileDto[] Files);

    private sealed record RustIndexedFileDto(string RelativePath, string ContentHash, long SizeBytes);
}
