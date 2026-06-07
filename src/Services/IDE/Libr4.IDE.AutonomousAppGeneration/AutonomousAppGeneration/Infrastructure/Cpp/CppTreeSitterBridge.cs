using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.Analysis;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Cpp;

/// <summary>Wave 6.1: C++ tree-sitter muscle — placeholder + complexity analysis via official C API.</summary>
internal static class CppTreeSitterBridge
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
                () => CppTreeSitterNative.libr4_ts_probe() == 0,
                out var probeOk,
                out _);
            _available = probeOk;
            return _available.Value;
        }
    }

    public static bool TryAnalyzeFile(
        string relativePath,
        string source,
        ILogger logger,
        out FileAnalysisResult? result)
    {
        result = null;
        if (!IsAvailable)
            return false;

        try
        {
            var rc = CppTreeSitterNative.libr4_ts_analyze_json(
                relativePath,
                source,
                string.Empty,
                out var jsonPtr);

            if (rc != 0 || jsonPtr == IntPtr.Zero)
                return false;

            var json = Marshal.PtrToStringUTF8(jsonPtr) ?? "{}";
            CppTreeSitterNative.libr4_ts_free_string(jsonPtr);

            var dto = JsonSerializer.Deserialize<CppAnalysisDto>(json, JsonOptions);
            if (dto is null)
                return false;

            result = Map(relativePath, dto);
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or BadImageFormatException or EntryPointNotFoundException)
        {
            _available = false;
            logger.LogDebug(ex, "[CppTreeSitter] native library unavailable");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[CppTreeSitter] analyze failed for {Path}", relativePath);
            return false;
        }
    }

    private static FileAnalysisResult Map(string path, CppAnalysisDto dto) =>
        new(
            Path: path,
            Complexity: dto.Complexity is null
                ? null
                : new ComplexityMetrics(
                    dto.Complexity.CyclomaticComplexity,
                    dto.Complexity.NestingDepth,
                    dto.Complexity.FunctionCount,
                    dto.Complexity.LinesOfCode),
            Placeholders: dto.Placeholders?
                .Select(p => new PlaceholderFinding(p.Line, p.Type, p.Message))
                .ToList()
                ?? [],
            SecurityIssues: [],
            TestQuality: null);

    private sealed record CppAnalysisDto(
        string? Language,
        bool ParseOk,
        CppComplexityDto? Complexity,
        CppPlaceholderDto[]? Placeholders);

    private sealed record CppComplexityDto(
        int CyclomaticComplexity,
        int NestingDepth,
        int FunctionCount,
        int LinesOfCode);

    private sealed record CppPlaceholderDto(int Line, string Type, string Message);
}
