using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class GlobTool : IAgentTool
{
    private const int MaxResults = 200;

    public string Name => "glob";
    public string Description => "List files by glob. Input: { \"pattern\": \"**/*.py\" }";
    public bool IsReadOnly => true;

    public bool IsConcurrencySafe(JsonElement input) => true;

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var pattern = input.TryGetProperty("pattern", out var patEl) && patEl.ValueKind == JsonValueKind.String
            ? patEl.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(pattern))
            return Task.FromResult(Fail("pattern is required"));

        var files = _workspaceGlob(context.Workspace.HostPath, pattern)
            .Where(f => !ShouldSkip(f))
            .Take(MaxResults)
            .Select(f => WorkspacePathHelper.ToRelative(context.Workspace.HostPath, f))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var output = files.Count == 0
            ? "no files"
            : string.Join("\n", files) + (files.Count >= MaxResults ? "\n...[truncated]..." : string.Empty);

        return Task.FromResult(new ToolExecutionResult(Name, true, output, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>()));
    }

    private static IEnumerable<string> _workspaceGlob(string root, string pattern)
    {
        pattern = pattern.Replace('\\', '/').TrimStart('/');
        if (pattern.Contains("..", StringComparison.Ordinal))
            yield break;

        if (!pattern.Contains('*') && !pattern.Contains('?'))
        {
            var single = Path.Combine(root, pattern.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(single))
                yield return single;
            yield break;
        }

        var dirPart = Path.GetDirectoryName(pattern.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty;
        var filePart = Path.GetFileName(pattern);
        var searchRoot = string.IsNullOrEmpty(dirPart)
            ? root
            : Path.Combine(root, dirPart);

        if (!Directory.Exists(searchRoot))
            yield break;

        foreach (var file in Directory.EnumerateFiles(searchRoot, filePart, SearchOption.AllDirectories))
            yield return file;
    }

    private static bool ShouldSkip(string path)
    {
        var p = path.Replace('\\', '/');
        return p.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/.git/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/__pycache__/", StringComparison.OrdinalIgnoreCase);
    }

    private static ToolExecutionResult Fail(string message) =>
        new("glob", false, message, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
