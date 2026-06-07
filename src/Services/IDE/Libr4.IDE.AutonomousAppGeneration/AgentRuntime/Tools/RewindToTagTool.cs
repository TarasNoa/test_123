using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.GitAutomation;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Rewind shadow workspace files to a prior repair git tag.</summary>
public sealed class RewindToTagTool : IAgentTool
{
    private readonly IShadowGitCheckpointService _git;

    public RewindToTagTool(IShadowGitCheckpointService git) => _git = git;

    public string Name => "rewind_to_tag";
    public string Description =>
        "Rewind shadow workspace to a repair git tag. Input: { \"tag\": \"repair-attempt-2\" } or { \"attempt\": 2 }";
    public bool IsReadOnly => false;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var tag = ResolveTag(input);
        if (string.IsNullOrWhiteSpace(tag))
            return Fail("tag or attempt is required");

        var hostPath = context.Workspace.HostPath;
        if (string.IsNullOrWhiteSpace(hostPath))
            return Fail("shadow workspace path unavailable");

        var ok = await _git.RewindToTagAsync(hostPath, tag, ct).ConfigureAwait(false);
        if (!ok)
            return Fail($"rewind failed: tag '{tag}' not found or git disabled");

        var patches = await ReloadWorkingFilesAsync(context, ct).ConfigureAwait(false);
        return new ToolExecutionResult(
            Name,
            true,
            $"rewound_to_tag={tag} files={patches.Count}",
            patches);
    }

    private static string? ResolveTag(JsonElement input)
    {
        if (input.TryGetProperty("tag", out var tagEl) && tagEl.ValueKind == JsonValueKind.String)
            return tagEl.GetString()?.Trim();

        if (input.TryGetProperty("attempt", out var attemptEl) && attemptEl.ValueKind == JsonValueKind.Number)
            return IShadowGitCheckpointService.RepairTagName(attemptEl.GetInt32());

        return null;
    }

    private static async Task<List<GeneratedFile>> ReloadWorkingFilesAsync(ToolContext context, CancellationToken ct)
    {
        var hostPath = context.Workspace.HostPath;
        if (string.IsNullOrWhiteSpace(hostPath) || !Directory.Exists(hostPath))
            return [];

        var patches = new List<GeneratedFile>();
        foreach (var path in EnumerateWorkspaceFiles(hostPath))
        {
            string content;
            try
            {
                content = await context.Accessor.ReadFileAsync(context.Workspace.WorkspaceId, path, ct)
                    .ConfigureAwait(false);
            }
            catch
            {
                continue;
            }

            var ext = Path.GetExtension(path).TrimStart('.');
            var language = string.IsNullOrEmpty(ext) ? "text" : ext;
            var file = new GeneratedFile(path, language, content);
            Upsert(context.WorkingFiles, file);
            patches.Add(file);
        }

        return patches;
    }

    private static IEnumerable<string> EnumerateWorkspaceFiles(string hostPath)
    {
        foreach (var abs in Directory.EnumerateFiles(hostPath, "*", SearchOption.AllDirectories))
        {
            if (abs.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;

            var rel = abs[(hostPath.Length + 1)..].Replace('\\', '/');
            if (rel.Equals(".gitignore", StringComparison.OrdinalIgnoreCase))
                continue;

            yield return rel;
        }
    }

    private static void Upsert(IList<GeneratedFile> files, GeneratedFile patch)
    {
        for (var i = 0; i < files.Count; i++)
        {
            if (files[i].RelativePath.Equals(patch.RelativePath, StringComparison.OrdinalIgnoreCase))
            {
                files[i] = patch;
                return;
            }
        }

        files.Add(patch);
    }

    private static ToolExecutionResult Fail(string message) =>
        new("rewind_to_tag", false, message, Array.Empty<GeneratedFile>());
}
