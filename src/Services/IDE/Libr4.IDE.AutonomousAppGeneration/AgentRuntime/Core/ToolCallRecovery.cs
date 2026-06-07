using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public enum ToolCallRecoveryStage
{
    RawContentCoercion,
    CompressedPrompt,
    StrictSchema,
    SchemaCoerce,
    BoilerplateFallback
}

public sealed record ToolCallRecoveryResult(
    AgentToolCall? ToolCall,
    string? SystemNudge,
    ToolCallRecoveryStage? Stage)
{
    public bool HasToolCall => ToolCall is not null;
    public bool RequiresNudge => ToolCall is null && !string.IsNullOrWhiteSpace(SystemNudge);
}

public static class ToolCallRecovery
{
    public const int CompressedPromptThreshold = 3;
    public const int StrictSchemaThreshold = 4;
    public const int SchemaCoerceThreshold = 4;
    public const int BoilerplateFallbackThreshold = 5;

    public static ToolCallRecoveryResult Recover(
        string rawResponse,
        int consecutiveInvalidTurns,
        IReadOnlyList<string>? targetPaths,
        IList<GeneratedFile>? workingFiles,
        GenerationPlan? plan,
        bool enableRawCoercion,
        bool enableBoilerplateFallback)
    {
        if (enableRawCoercion)
        {
            var coerced = AgentGenerationPolicy.TryCoerceWriteFileFromRaw(rawResponse, targetPaths);
            if (coerced is not null)
                return new ToolCallRecoveryResult(coerced, null, ToolCallRecoveryStage.RawContentCoercion);
        }

        if (enableBoilerplateFallback
            && consecutiveInvalidTurns >= BoilerplateFallbackThreshold
            && targetPaths?.Count == 1)
        {
            var boilerplate = BoilerplateRegistry.TryCreateWriteCall(
                targetPaths[0],
                workingFiles,
                plan);
            if (boilerplate is not null)
                return new ToolCallRecoveryResult(boilerplate, null, ToolCallRecoveryStage.BoilerplateFallback);
        }

        if (targetPaths?.Count == 1)
        {
            var path = targetPaths[0];
            if (consecutiveInvalidTurns >= SchemaCoerceThreshold)
            {
                var coercedTool = TrySchemaCoerceWriteFile(rawResponse, path);
                if (coercedTool is not null)
                    return new ToolCallRecoveryResult(coercedTool, null, ToolCallRecoveryStage.SchemaCoerce);
            }

            if (consecutiveInvalidTurns >= StrictSchemaThreshold)
            {
                return new ToolCallRecoveryResult(
                    null,
                    BuildStrictSchemaNudge(path),
                    ToolCallRecoveryStage.StrictSchema);
            }

            if (consecutiveInvalidTurns >= CompressedPromptThreshold)
            {
                return new ToolCallRecoveryResult(
                    null,
                    BuildCompressedPromptNudge(path),
                    ToolCallRecoveryStage.CompressedPrompt);
            }
        }

        return new ToolCallRecoveryResult(
            null,
            "Invalid response. Reply ONLY with JSON: {\"action\":\"tool\",...} or {\"action\":\"done\",...}",
            null);
    }

    public static string BuildCompressedPromptNudge(string targetPath)
    {
        var path = FixerPatchScopePolicy.NormalizePatchRelativePath(targetPath);
        var writeExample =
            "{\"action\":\"tool\",\"tool\":\"write_file\",\"input\":{\"path\":\"" + path +
            "\",\"content\":\"<FULL FILE>\"}}";
        return $"""
            PROTOCOL RECOVERY (compressed). Invalid JSON detected repeatedly.
            Target file: {path}
            Respond with EXACTLY one JSON object — no markdown, no prose, no raw source outside JSON.
            Required next action:
            {writeExample}
            """;
    }

    private static AgentToolCall? TrySchemaCoerceWriteFile(string raw, string targetPath)
    {
        var path = FixerPatchScopePolicy.NormalizePatchRelativePath(targetPath);
        if (!raw.Contains(path, StringComparison.OrdinalIgnoreCase))
            return null;

        var content = raw.Trim();
        if (content.StartsWith('{'))
            return null;

        return AgentGenerationPolicy.TryCoerceWriteFileFromRaw(content, new[] { path });
    }

    public static string BuildStrictSchemaNudge(string targetPath)
    {
        var path = FixerPatchScopePolicy.NormalizePatchRelativePath(targetPath);
        var writeShape =
            "{\"action\":\"tool\",\"tool\":\"write_file\",\"input\":{\"path\":\"" + path + "\",\"content\":string}}";
        var doneShape = "{\"action\":\"done\",\"summary\":string}";
        return $"""
            PROTOCOL RECOVERY (strict schema). Your last responses were not valid agent JSON.
            JSON schema (only allowed shape):
            type AgentTurn = {writeShape} | {doneShape}
            Constraints:
            - "action" must be "tool" or "done"
            - for write_file: "path" must be "{path}", "content" must be complete file text
            - no extra keys at root level
            Emit write_file now.
            """;
    }
}
