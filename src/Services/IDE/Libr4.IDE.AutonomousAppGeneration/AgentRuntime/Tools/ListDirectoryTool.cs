using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Pathing;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

public sealed class ListDirectoryTool : IAgentTool
{
    private readonly IWorkspacePathValidator _paths;

    public ListDirectoryTool(IWorkspacePathValidator paths) => _paths = paths;

    public string Name => "list_directory";
    public string Description => "List workspace directory tree. Input: { \"path\": \".\", \"depth\": 2 }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var rel = input.TryGetProperty("path", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "." : ".";
        var depth = input.TryGetProperty("depth", out var d) && d.TryGetInt32(out var dv) ? Math.Clamp(dv, 1, 6) : 2;
        var validation = _paths.Validate(rel, new ToolContextPaths(context.Workspace.HostPath, context.Session.RunId));
        if (!validation.Allowed)
        {
            _paths.AuditDenied(validation, Name, context.Session.RunId);
            return Task.FromResult(Fail(validation.DenyReason ?? "denied"));
        }

        var root = Path.Combine(context.Workspace.HostPath, validation.NormalizedPath.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(root))
            return Task.FromResult(Fail($"directory not found: {validation.NormalizedPath}"));

        var sb = new StringBuilder();
        AppendTree(sb, root, validation.NormalizedPath, depth, 0);
        return Task.FromResult(new ToolExecutionResult(Name, true, sb.ToString(), Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>()));
    }

    private static void AppendTree(StringBuilder sb, string absDir, string relDir, int maxDepth, int depth)
    {
        sb.Append(relDir).Append('/').AppendLine();
        if (depth >= maxDepth)
            return;

        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(absDir).OrderBy(e => e, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return;
        }

        foreach (var entry in entries)
        {
            var name = Path.GetFileName(entry);
            if (name.StartsWith('.') && name is not ".gitignore")
                continue;
            var childRel = string.IsNullOrEmpty(relDir) || relDir == "." ? name : $"{relDir}/{name}";
            if (Directory.Exists(entry))
                AppendTree(sb, entry, childRel, maxDepth, depth + 1);
            else
                sb.Append("  ".PadLeft(depth + 1)).Append(childRel).AppendLine();
        }
    }

    private static ToolExecutionResult Fail(string msg) =>
        new("list_directory", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
