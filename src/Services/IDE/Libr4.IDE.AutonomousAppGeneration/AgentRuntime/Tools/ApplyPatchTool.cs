using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Patching;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Pathing;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class ApplyPatchTool : IAgentTool
{
    private readonly IWorkspacePathValidator _paths;
    private readonly IPatchAttemptRecorder _recorder;

    public ApplyPatchTool(IWorkspacePathValidator paths, IPatchAttemptRecorder recorder)
    {
        _paths = paths;
        _recorder = recorder;
    }

    public string Name => "apply_patch";
    public string Description => "Apply unified diff patch. Input: { \"path\": \"file.py\", \"patch\": \"@@ ...\" }";
    public bool IsReadOnly => false;
    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        if (!input.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String)
            return Fail("path required");
        if (!input.TryGetProperty("patch", out var patchEl) || patchEl.ValueKind != JsonValueKind.String)
            return Fail("patch required");

        var validation = _paths.Validate(pathEl.GetString()!, new ToolContextPaths(context.Workspace.HostPath, context.Session.RunId));
        if (!validation.Allowed)
        {
            _paths.AuditDenied(validation, Name, context.Session.RunId);
            return Fail(validation.DenyReason ?? "denied");
        }

        var path = validation.NormalizedPath;
        var patch = patchEl.GetString() ?? string.Empty;
        var existing = context.WorkingFiles.FirstOrDefault(f =>
            string.Equals(FixerPatchScopePolicy.NormalizePatchRelativePath(f.RelativePath), path, StringComparison.OrdinalIgnoreCase));
        var original = existing?.Content ?? string.Empty;

        var diff = UnifiedDiffParser.Parse(patch, path);
        var result = PatchApplicator.ApplyFuzzy(original, diff);
        await _recorder.RecordAsync(context.Session.RunId, path, patch, result, ct).ConfigureAwait(false);

        if (!result.Success)
            return Fail(result.ConflictReport ?? "patch failed");

        var file = PatchApplicator.ToGeneratedFile(path, result, existing)
                   ?? new GeneratedFile(path, existing?.Language, result.PatchedContent!);
        return new ToolExecutionResult(Name, true, $"patched {path} via {result.Mode}", new[] { file });
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("apply_patch", false, msg, Array.Empty<GeneratedFile>());
}
