using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class EditFileTool : IAgentTool
{

    public string Name => "edit_file";
    public string Description => "Search/replace edit. Input: { \"path\", \"search\", \"replace\" }";
    public bool IsReadOnly => false;

    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var path = GetString(input, "path");
        var search = GetString(input, "search");
        var replace = GetString(input, "replace") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(path))
            return Fail("path is required");
        if (string.IsNullOrEmpty(search))
            return Fail("search is required");

        path = FixerPatchScopePolicy.NormalizePatchRelativePath(path);
        var result = SurgicalPatchEngine.Apply(
            context.WorkingFiles.ToList(),
            new[] { new SurgicalPatchEngine.SurgicalEdit(path, search, replace) });

        if (result.AppliedEdits == 0)
        {
            var warn = result.Warnings.Count > 0 ? string.Join("; ", result.Warnings) : "no edits applied";
            return Fail(warn);
        }

        foreach (var patch in result.Patches)
        {
            await context.Accessor.WriteFileAsync(context.Workspace.WorkspaceId, patch.RelativePath, patch.Content ?? string.Empty, ct)
                .ConfigureAwait(false);
            UpsertWorkingFile(context.WorkingFiles, patch);
            context.FileState.RecordRead(patch.RelativePath, patch.Content ?? string.Empty, DateTime.UtcNow);
        }

        return new ToolExecutionResult(
            Name,
            true,
            $"applied={result.AppliedEdits} skipped={result.SkippedEdits} warnings={result.Warnings.Count}",
            result.Patches);
    }

    private static void UpsertWorkingFile(IList<GeneratedFile> files, GeneratedFile patch)
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

    private static string? GetString(JsonElement input, string name) =>
        input.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static ToolExecutionResult Fail(string message) =>
        new("edit_file", false, message, Array.Empty<GeneratedFile>());
}
