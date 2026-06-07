using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;
using DomainGeneratedFile = Libr4.IDE.Domain.AutonomousAppGeneration.GeneratedFile;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Jit;

public interface IContextInjector
{
    bool TryInjectForPath(
        string accessedRelativePath,
        string workspaceHostPath,
        IReadOnlyList<DomainGeneratedFile>? workingFiles,
        out string formattedContext);

    string ResolveMergedContext(
        string accessedRelativePath,
        string workspaceHostPath,
        IReadOnlyList<DomainGeneratedFile>? workingFiles);
}

public sealed class Libr4ContextInjector : IContextInjector
{
    public const string Libr4FileName = "LIBR4.md";
    public const string Libr4OverrideFileName = "LIBR4.override.md";

    private readonly Libr4ContextOptions _options;

    public Libr4ContextInjector(IOptions<Libr4ContextOptions> options) => _options = options.Value;

    public bool TryInjectForPath(
        string accessedRelativePath,
        string workspaceHostPath,
        IReadOnlyList<DomainGeneratedFile>? workingFiles,
        out string formattedContext)
    {
        formattedContext = string.Empty;
        if (!_options.EnableJitInjection)
            return false;

        var merged = ResolveMergedContext(accessedRelativePath, workspaceHostPath, workingFiles);
        if (string.IsNullOrWhiteSpace(merged))
            return false;

        formattedContext = merged.Length <= _options.MaxCharsPerInjection
            ? merged
            : merged[.._options.MaxCharsPerInjection] + "\n…";
        return true;
    }

    public string ResolveMergedContext(
        string accessedRelativePath,
        string workspaceHostPath,
        IReadOnlyList<DomainGeneratedFile>? workingFiles)
    {
        var normalized = FixerPatchScopePolicy.NormalizePatchRelativePath(accessedRelativePath);
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        var byPath = workingFiles?.ToDictionary(
            f => f.RelativePath.Replace('\\', '/'),
            f => f.Content ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

        var sb = new System.Text.StringBuilder();
        foreach (var directory in BuildDirectoryChain(normalized))
        {
            AppendArtifact(sb, directory, Libr4FileName, workspaceHostPath, byPath);
            AppendArtifact(sb, directory, Libr4OverrideFileName, workspaceHostPath, byPath, isOverride: true);
        }

        return sb.ToString().Trim();
    }

    private static IEnumerable<string> BuildDirectoryChain(string relativePath)
    {
        yield return string.Empty;

        var dir = Path.GetDirectoryName(relativePath.Replace('\\', '/'))?.Replace('\\', '/') ?? string.Empty;
        if (string.IsNullOrEmpty(dir))
            yield break;

        var parts = dir.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
            yield return string.Join('/', parts.Take(i + 1));
    }

    private static void AppendArtifact(
        System.Text.StringBuilder sb,
        string directory,
        string fileName,
        string workspaceHostPath,
        IReadOnlyDictionary<string, string>? workingFiles,
        bool isOverride = false)
    {
        var relative = string.IsNullOrEmpty(directory) ? fileName : $"{directory}/{fileName}";
        if (!TryReadContent(relative, workspaceHostPath, workingFiles, out var content))
            return;

        var label = isOverride ? $"{relative} (override)" : relative;
        sb.AppendLine($"## {label}");
        sb.AppendLine(content.Trim());
        sb.AppendLine();
    }

    private static bool TryReadContent(
        string relativePath,
        string workspaceHostPath,
        IReadOnlyDictionary<string, string>? workingFiles,
        out string content)
    {
        content = string.Empty;
        if (workingFiles is not null && workingFiles.TryGetValue(relativePath, out var inMemory))
        {
            content = inMemory;
            return !string.IsNullOrWhiteSpace(content);
        }

        if (string.IsNullOrWhiteSpace(workspaceHostPath))
            return false;

        var abs = WorkspacePathHelper.ResolveHostPath(workspaceHostPath, relativePath);
        if (!File.Exists(abs))
            return false;

        content = File.ReadAllText(abs);
        return !string.IsNullOrWhiteSpace(content);
    }
}

public static class Libr4MdManifest
{
    public static void AppendForPlan(
        GenerationPlan plan,
        IList<Libr4.IDE.AutonomousAppGeneration.Agents.PlannedFileEntry> entries)
    {
        if (!StackLayoutHeuristics.UsesDjango(plan))
            return;

        entries.Add(Entry("LIBR4.md", Libr4.IDE.AutonomousAppGeneration.Agents.AgentPhase.Backend, "Repo-wide Libr4 agent guidance.", "markdown"));
        entries.Add(Entry("backend/LIBR4.override.md", Libr4.IDE.AutonomousAppGeneration.Agents.AgentPhase.Backend, "Backend-specific Libr4 overrides.", "markdown"));

        if (StackLayoutHeuristics.HasSeparatedFrontend(plan))
            entries.Add(Entry("frontend/LIBR4.override.md", Libr4.IDE.AutonomousAppGeneration.Agents.AgentPhase.Frontend, "Frontend-specific Libr4 overrides.", "markdown"));
    }

    public static IReadOnlyList<DomainGeneratedFile> SeedContentForPlan(GenerationPlan plan)
    {
        if (!StackLayoutHeuristics.UsesDjango(plan))
            return Array.Empty<DomainGeneratedFile>();

        var files = new List<DomainGeneratedFile>
        {
            new("LIBR4.md", "markdown", Libr4MdTemplates.Root.Trim()),
            new("backend/LIBR4.override.md", "markdown", Libr4MdTemplates.BackendOverride.Trim())
        };

        if (StackLayoutHeuristics.HasSeparatedFrontend(plan))
            files.Add(new DomainGeneratedFile("frontend/LIBR4.override.md", "markdown", Libr4MdTemplates.FrontendOverride.Trim()));

        return files;
    }

    private static Libr4.IDE.AutonomousAppGeneration.Agents.PlannedFileEntry Entry(
        string path,
        Libr4.IDE.AutonomousAppGeneration.Agents.AgentPhase phase,
        string description,
        string role) =>
        new(path, phase, description, role);
}
