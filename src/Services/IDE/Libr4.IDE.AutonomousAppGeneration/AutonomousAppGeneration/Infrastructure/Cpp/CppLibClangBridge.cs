using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;

/// <summary>Wave 6.3: libclang C API for C/C++ repo import analysis.</summary>
internal static class CppLibClangBridge
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
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

            _available = CppNativeLibrary.TryLoad(
                () => CppLibClangNative.libr4_cl_probe() == 0,
                out var ok,
                out _);
            _available = ok;
            return _available.Value;
        }
    }

    public static bool TryParseIncludes(
        string relativePath,
        string source,
        ILogger logger,
        out IReadOnlyList<string> includes,
        out int functionCount,
        out int linesOfCode)
    {
        includes = Array.Empty<string>();
        functionCount = 0;
        linesOfCode = 0;

        if (!IsAvailable || string.IsNullOrWhiteSpace(relativePath))
            return false;

        try
        {
            var rc = CppLibClangNative.libr4_cl_parse_repo_json(
                relativePath,
                source ?? string.Empty,
                out var jsonPtr);

            if (rc != 0 || jsonPtr == IntPtr.Zero)
                return false;

            var json = Marshal.PtrToStringUTF8(jsonPtr) ?? "{}";
            CppLibClangNative.libr4_cl_free_string(jsonPtr);

            var dto = JsonSerializer.Deserialize<CppRepoDto>(json, JsonOptions);
            if (dto is null)
                return false;

            includes = dto.Includes ?? Array.Empty<string>();
            functionCount = dto.FunctionCount;
            linesOfCode = dto.LinesOfCode;
            return dto.ParseOk || includes.Count > 0;
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            _available = false;
            logger.LogDebug(ex, "[CppLibClang] native library unavailable");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[CppLibClang] parse failed for {Path}", relativePath);
            return false;
        }
    }

    private sealed record CppRepoDto(
        bool ParseOk,
        int FunctionCount,
        int LinesOfCode,
        string[]? Includes);
}
