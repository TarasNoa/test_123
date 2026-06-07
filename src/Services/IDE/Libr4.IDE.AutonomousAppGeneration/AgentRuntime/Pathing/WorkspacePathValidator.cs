using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Pathing;

public sealed class WorkspacePathValidator : IWorkspacePathValidator
{
    private readonly AgentRuntimeOptions _options;
    private readonly IPathAccessAudit _audit;

    public WorkspacePathValidator(IOptions<AgentRuntimeOptions> options, IPathAccessAudit audit)
    {
        _options = options.Value;
        _audit = audit;
    }

    public PathValidationResult Validate(string relativePath, ToolContextPaths context)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return new PathValidationResult(false, string.Empty, "empty path");

        var normalized = FixerPatchScopePolicy.NormalizePatchRelativePath(relativePath);
        if (normalized.Contains("..", StringComparison.Ordinal))
            return new PathValidationResult(false, normalized, "path traversal");

        if (Path.IsPathRooted(normalized))
            return new PathValidationResult(false, normalized, "absolute paths denied");

        foreach (var pattern in _options.DeniedPathPatterns)
        {
            if (MatchesGlob(normalized, pattern))
                return new PathValidationResult(false, normalized, $"denied pattern: {pattern}");
        }

        var full = Path.GetFullPath(Path.Combine(context.WorkspaceRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(context.WorkspaceRoot);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return new PathValidationResult(false, normalized, "outside workspace root");

        if (File.Exists(full))
        {
            var info = new FileInfo(full);
            if (info.LinkTarget is not null && !_options.AllowSymlinks)
                return new PathValidationResult(false, normalized, "symlink denied");
        }

        return new PathValidationResult(true, normalized, null);
    }

    public void AuditDenied(PathValidationResult result, string toolName, Guid? runId)
    {
        if (result.Allowed || string.IsNullOrWhiteSpace(result.DenyReason))
            return;
        _audit.RecordDenied(toolName, result.NormalizedPath, result.DenyReason, runId);
    }

    private static bool MatchesGlob(string path, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        var normalizedPath = path.TrimStart('/', '\\');
        var normalizedPattern = pattern.TrimStart('/', '\\');
        if (normalizedPattern.Contains('*'))
        {
            var prefix = normalizedPattern.TrimEnd('*').TrimEnd('/', '\\');
            return normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(normalizedPath, normalizedPattern, StringComparison.OrdinalIgnoreCase);
    }
}
