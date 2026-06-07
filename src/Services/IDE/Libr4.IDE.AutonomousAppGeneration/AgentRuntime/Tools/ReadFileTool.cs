using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class ReadFileTool : IAgentTool
{
    private readonly AgentRuntimeOptions _options;

    public ReadFileTool(IOptions<AgentRuntimeOptions> options) => _options = options.Value;

    public string Name => "read_file";
    public string Description => "Read a file from the shadow workspace. Input: { \"path\": \"backend/...\" }";
    public bool IsReadOnly => true;

    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        if (!input.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String)
            return Fail("path is required");

        var path = FixerPatchScopePolicy.NormalizePatchRelativePath(pathEl.GetString() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(path))
            return Fail("path is empty");

        var abs = WorkspacePathHelper.ResolveHostPath(context.Workspace.HostPath, path);
        if (!File.Exists(abs))
            return Fail($"file not found: {path}");

        var info = new FileInfo(abs);
        if (info.Length > _options.MaxReadFileBytes)
            return Fail($"file too large ({info.Length} bytes, max {_options.MaxReadFileBytes})");

        var content = await File.ReadAllTextAsync(abs, Encoding.UTF8, ct).ConfigureAwait(false);
        context.FileState.RecordRead(path, content, info.LastWriteTimeUtc);

        var numbered = NumberLines(content);
        return new ToolExecutionResult(Name, true, $"path={path}\nlines={CountLines(content)}\n---\n{numbered}", Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
    }

    private static ToolExecutionResult Fail(string message) =>
        new("read_file", false, message, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());

    private static string NumberLines(string content)
    {
        var lines = content.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
            sb.Append(i + 1).Append('|').AppendLine(lines[i]);
        return sb.ToString().TrimEnd();
    }

    private static int CountLines(string content) =>
        string.IsNullOrEmpty(content) ? 0 : content.Replace("\r\n", "\n").Split('\n').Length;
}
