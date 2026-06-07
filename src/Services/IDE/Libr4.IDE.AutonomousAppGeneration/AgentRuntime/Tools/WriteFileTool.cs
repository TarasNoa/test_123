using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class WriteFileTool : IAgentTool
{

    public string Name => "write_file";
    public string Description => "Create or overwrite a file. Input: { \"path\", \"content\" }";
    public bool IsReadOnly => false;

    public bool IsConcurrencySafe(JsonElement input) => false;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var path = input.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String
            ? pathEl.GetString()
            : null;
        var content = input.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String
            ? contentEl.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(path))
            return Fail("path is required");
        if (content is null)
            return Fail("content is required");

        path = FixerPatchScopePolicy.NormalizePatchRelativePath(path);
        await context.Accessor.WriteFileAsync(context.Workspace.WorkspaceId, path, content, ct).ConfigureAwait(false);

        var patch = new GeneratedFile(path, InferLanguage(path), content);
        UpsertWorkingFile(context.WorkingFiles, patch);
        context.FileState.RecordRead(path, content, DateTime.UtcNow);

        return new ToolExecutionResult(Name, true, $"wrote {path} ({content.Length} chars)", new[] { patch });
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

    private static string InferLanguage(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".cs" => "csharp",
            ".java" => "java",
            ".ts" or ".tsx" => "typescript",
            ".js" or ".jsx" => "javascript",
            ".py" => "python",
            ".json" => "json",
            _ => "text"
        };
    }

    private static ToolExecutionResult Fail(string message) =>
        new("write_file", false, message, Array.Empty<GeneratedFile>());
}
