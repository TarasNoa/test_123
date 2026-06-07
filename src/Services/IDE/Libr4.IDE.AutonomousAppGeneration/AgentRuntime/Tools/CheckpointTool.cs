using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Claude Code /resume-style checkpoint in agent session.</summary>
public sealed class CheckpointTool : IAgentTool
{
    public string Name => "checkpoint";
    public string Description => "Save/restore workspace files. Input: { \"action\":\"create\"|\"restore\", \"label\"? }";
    public bool IsReadOnly => false;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var action = input.TryGetProperty("action", out var a) && a.ValueKind == JsonValueKind.String
            ? a.GetString()?.Trim().ToLowerInvariant()
            : null;
        if (action is not ("create" or "restore"))
            return Fail("action must be create or restore");

        if (action == "create")
        {
            var label = input.TryGetProperty("label", out var l) && l.ValueKind == JsonValueKind.String
                ? l.GetString() ?? "agent_checkpoint"
                : "agent_checkpoint";
            var id = Guid.NewGuid().ToString("N");
            var files = context.WorkingFiles.ToDictionary(f => f.RelativePath, f => f, StringComparer.OrdinalIgnoreCase);
            var snap = new CheckpointSnapshot(id, label, DateTime.UtcNow, files);
            context.Session.Checkpoints[id] = snap;
            context.Session.LastCheckpointId = id;
            return new ToolExecutionResult(Name, true, $"checkpoint_created id={id} files={files.Count}", Array.Empty<GeneratedFile>());
        }

        var restoreId = input.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
            ? idEl.GetString()
            : context.Session.LastCheckpointId;
        if (string.IsNullOrWhiteSpace(restoreId) || !context.Session.Checkpoints.TryGetValue(restoreId, out var snapshot))
            return Fail("checkpoint not found");

        var patches = new List<GeneratedFile>();
        foreach (var file in snapshot.FilesByPath.Values)
        {
            await context.Accessor.WriteFileAsync(
                context.Workspace.WorkspaceId,
                file.RelativePath,
                file.Content ?? string.Empty,
                ct).ConfigureAwait(false);
            Upsert(context.WorkingFiles, file);
            patches.Add(file);
        }

        return new ToolExecutionResult(
            Name,
            true,
            $"checkpoint_restored id={restoreId} files={patches.Count}",
            patches);
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

    private static ToolExecutionResult Fail(string msg) =>
        new("checkpoint", false, msg, Array.Empty<GeneratedFile>());
}
