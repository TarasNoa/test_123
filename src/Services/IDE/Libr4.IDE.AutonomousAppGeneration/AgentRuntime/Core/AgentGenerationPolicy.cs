using System.Text.Json;
using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public static partial class AgentGenerationPolicy
{
    public static bool TargetsSatisfied(
        IReadOnlyDictionary<string, GeneratedFile> patches,
        IReadOnlyList<string>? targetPaths,
        int minChars = 20)
    {
        return FSharpAlgorithmsBridge.TargetsSatisfied(patches, targetPaths, minChars);
    }

    public static bool WorkspaceFileExists(ToolContext context, string normalizedPath)
    {
        if (context.WorkingFiles.Any(f =>
                string.Equals(
                    FixerPatchScopePolicy.NormalizePatchRelativePath(f.RelativePath),
                    normalizedPath,
                    StringComparison.OrdinalIgnoreCase)))
            return true;

        try
        {
            var abs = WorkspacePathHelper.ResolveHostPath(context.Workspace.HostPath, normalizedPath);
            return File.Exists(abs);
        }
        catch
        {
            return false;
        }
    }

    public static bool RequiresReadBeforeWrite(ToolContext context, string toolName, string path)
    {
        var normalized = FixerPatchScopePolicy.NormalizePatchRelativePath(path);
        if (context.FileState.HasRead(normalized))
            return false;

        if (string.Equals(toolName, "edit_file", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(toolName, "write_file", StringComparison.OrdinalIgnoreCase))
            return WorkspaceFileExists(context, normalized);

        return false;
    }

    /// <summary>
    /// When the model dumps raw source instead of JSON tool syntax, coerce to write_file.
    /// </summary>
    public static AgentToolCall? TryCoerceWriteFileFromRaw(string raw, IReadOnlyList<string>? targetPaths)
    {
        if (targetPaths is null || targetPaths.Count != 1)
            return null;

        var content = ExtractCoercibleSource(raw);
        if (content is null || content.Length < 60)
            return null;

        var path = FixerPatchScopePolicy.NormalizePatchRelativePath(targetPaths[0]);
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new { path, content }));
        return new AgentToolCall("write_file", doc.RootElement.Clone());
    }

    public static string BuildInvalidJsonNudge(string targetPath)
    {
        var path = FixerPatchScopePolicy.NormalizePatchRelativePath(targetPath);
        var writeExample =
            "{\"action\":\"tool\",\"tool\":\"write_file\",\"input\":{\"path\":\"" + path +
            "\",\"content\":\"<FULL FILE CONTENT HERE>\"}}";
        var doneExample = "{\"action\":\"done\",\"summary\":\"written " + path + "\"}";
        return $"""
            Invalid response format. You MUST emit ONLY one JSON object per turn.
            Do NOT return raw Python/TypeScript source outside JSON.
            Example for target "{path}":
            {writeExample}
            Then on next turn after write succeeds: {doneExample}
            """;
    }

    private static string? ExtractCoercibleSource(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var trimmed = raw.Trim();
        if (trimmed.StartsWith('{') && trimmed.Contains("\"action\"", StringComparison.OrdinalIgnoreCase))
            return null;

        var fenced = MarkdownFenceRegex().Match(trimmed);
        if (fenced.Success)
            trimmed = fenced.Groups[1].Value.Trim();

        if (!LooksLikeSourceCode(trimmed))
            return null;

        return trimmed;
    }

    private static bool LooksLikeSourceCode(string text)
    {
        if (!text.Contains('\n') && text.Length < 120)
            return false;

        return SourceCodeMarkerRegex().IsMatch(text);
    }

    [GeneratedRegex(@"```(?:\w+)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase)]
    private static partial Regex MarkdownFenceRegex();

    [GeneratedRegex(
        @"(^|\n)\s*(import |from |def |class |export |const |function |interface |type |#include|///|""use strict""|@)",
        RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex SourceCodeMarkerRegex();
}
