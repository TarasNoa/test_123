using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class GrepTool : IAgentTool
{
    private const int MaxMatches = 80;
    private const int MaxFileBytes = 256_000;

    public string Name => "grep";
    public string Description => "Search file contents. Input: { \"pattern\": \"regex\", \"path\": \"optional/glob/dir\" }";
    public bool IsReadOnly => true;

    public bool IsConcurrencySafe(JsonElement input) => true;

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var pattern = input.TryGetProperty("pattern", out var patEl) && patEl.ValueKind == JsonValueKind.String
            ? patEl.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(pattern))
            return Task.FromResult(Fail("pattern is required"));

        var scope = input.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String
            ? pathEl.GetString()
            : null;

        Regex regex;
        try
        {
            regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Fail($"invalid regex: {ex.Message}"));
        }

        var root = context.Workspace.HostPath;
        var searchRoot = string.IsNullOrWhiteSpace(scope)
            ? root
            : WorkspacePathHelper.ResolveHostPath(root, scope);

        if (!Directory.Exists(searchRoot) && File.Exists(searchRoot))
        {
            var single = SearchFile(searchRoot, regex, context.Workspace.HostPath);
            return Task.FromResult(new ToolExecutionResult(Name, true, single, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>()));
        }

        if (!Directory.Exists(searchRoot))
            return Task.FromResult(Fail($"path not found: {scope ?? "."}"));

        var matches = new List<string>();
        foreach (var file in Directory.EnumerateFiles(searchRoot, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            if (ShouldSkip(file))
                continue;

            var info = new FileInfo(file);
            if (info.Length > MaxFileBytes)
                continue;

            var rel = WorkspacePathHelper.ToRelative(root, file);
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (!regex.IsMatch(lines[i]))
                    continue;

                matches.Add($"{rel}:{i + 1}:{lines[i].Trim()}");
                if (matches.Count >= MaxMatches)
                    break;
            }

            if (matches.Count >= MaxMatches)
                break;
        }

        var output = matches.Count == 0
            ? "no matches"
            : string.Join("\n", matches) + (matches.Count >= MaxMatches ? "\n...[truncated]..." : string.Empty);

        return Task.FromResult(new ToolExecutionResult(Name, true, output, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>()));
    }

    private static string SearchFile(string absPath, Regex regex, string workspaceRoot)
    {
        var rel = WorkspacePathHelper.ToRelative(workspaceRoot, absPath);
        var info = new FileInfo(absPath);
        if (info.Length > MaxFileBytes)
            return $"file too large: {rel}";

        var lines = File.ReadAllLines(absPath);
        var hits = new List<string>();
        for (var i = 0; i < lines.Length; i++)
        {
            if (regex.IsMatch(lines[i]))
                hits.Add($"{rel}:{i + 1}:{lines[i].Trim()}");
            if (hits.Count >= MaxMatches)
                break;
        }

        return hits.Count == 0 ? "no matches" : string.Join("\n", hits);
    }

    private static bool ShouldSkip(string path)
    {
        var p = path.Replace('\\', '/');
        return p.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/.git/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/__pycache__/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/dist/", StringComparison.OrdinalIgnoreCase)
               || p.Contains("/.venv/", StringComparison.OrdinalIgnoreCase);
    }

    private static ToolExecutionResult Fail(string message) =>
        new("grep", false, message, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
