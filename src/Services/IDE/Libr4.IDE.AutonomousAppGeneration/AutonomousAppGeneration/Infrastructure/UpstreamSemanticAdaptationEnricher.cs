using System.Text;
using System.Text.RegularExpressions;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Deterministic extraction of upstream domain hints (board/column/task semantics) into product C# artifacts.
/// </summary>
public static class UpstreamSemanticAdaptationEnricher
{
    private static readonly Regex ExportEnum = new(
        @"export\s+enum\s+(\w+)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(300));

    private static readonly Regex ExportInterface = new(
        @"export\s+interface\s+(\w+)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(300));

    private static readonly Regex ExportType = new(
        @"export\s+type\s+(\w+)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(300));

    private static readonly Regex ConstArray = new(
        @"const\s+(\w*(?:column|board|lane|status|stage)\w*)\s*=\s*\[([^\]]{1,400})\]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(300));

    public static IReadOnlyList<GeneratedFile> BuildPatches(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> currentFiles) =>
        BuildPatches(plan, currentFiles, out _);

    public static int Apply(GenerationPlan plan, IList<GeneratedFile> files)
    {
        var patches = BuildPatches(plan, files as IReadOnlyList<GeneratedFile> ?? files.ToList(), out _);
        var changed = 0;
        foreach (var patch in patches)
            changed += Upsert(files, patch);
        return changed;
    }

    public static IReadOnlyList<GeneratedFile> BuildPatches(
        GenerationPlan plan,
        IReadOnlyList<GeneratedFile> currentFiles,
        out UpstreamSemanticDigest digest)
    {
        digest = ExtractDigest(currentFiles);
        if (digest.UpstreamFileCount == 0)
            return Array.Empty<GeneratedFile>();

        var upstreamInputs = currentFiles
            .Where(f => f.RelativePath.Replace('\\', '/').StartsWith("upstream/", StringComparison.OrdinalIgnoreCase))
            .Select(f => (f.RelativePath.Replace('\\', '/'), f.Content ?? string.Empty))
            .ToList();
        var domainMap = TypeScriptToCSharpDomainMapper.MapUpstreamFiles(upstreamInputs);

        var root = currentFiles
            .Select(f => f.RelativePath.Replace('\\', '/'))
            .Where(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains("/tests/", StringComparison.OrdinalIgnoreCase))
            .Select(p => p[..p.LastIndexOf('/')])
            .FirstOrDefault()
            ?? "src/GeneratedApp.Api";

        var ns = BuildNamespaceFromRoot(root);
        var patches = new List<GeneratedFile>
        {
            new("UPSTREAM_SEMANTIC_EXTRACT.md", "markdown", BuildExtractMarkdown(digest, domainMap)),
            new($"{root}/Domain/UpstreamSemanticMap.cs", "csharp", BuildSemanticMapFile(ns, digest, domainMap))
        };

        var adaptedTypes = TypeScriptToCSharpDomainMapper.GenerateCSharpFile(ns, domainMap);
        if (!string.IsNullOrWhiteSpace(adaptedTypes))
            patches.Add(new($"{root}/Domain/UpstreamAdaptedTypes.cs", "csharp", adaptedTypes));

        var servicePath = $"{root}/Services/KanbanBoardService.cs";
        var service = currentFiles.FirstOrDefault(f =>
            f.RelativePath.Equals(servicePath, StringComparison.OrdinalIgnoreCase));
        if (service is not null)
            patches.Add(new GeneratedFile(servicePath, "csharp", EnrichKanbanService(service.Content ?? string.Empty, ns, digest)));

        _ = plan;
        return patches;
    }

    public static string BuildUpstreamDigestForLlm(IReadOnlyList<GeneratedFile> files, int maxChars = 14_000)
    {
        var digest = ExtractDigest(files);
        var sb = new StringBuilder();
        var upstreamInputs = files
            .Where(f => f.RelativePath.Replace('\\', '/').StartsWith("upstream/", StringComparison.OrdinalIgnoreCase))
            .Select(f => (f.RelativePath.Replace('\\', '/'), f.Content ?? string.Empty))
            .ToList();
        var domainMap = TypeScriptToCSharpDomainMapper.MapUpstreamFiles(upstreamInputs);

        sb.AppendLine("UPSTREAM SEMANTIC ADAPTATION REQUEST");
        sb.AppendLine($"Repository concepts: {string.Join(", ", digest.Concepts)}");
        sb.AppendLine($"Discovered types: {string.Join(", ", digest.ExportedTypes)}");
        sb.AppendLine($"Column labels: {string.Join(", ", digest.ColumnLabels)}");
        sb.AppendLine();
        sb.AppendLine(TypeScriptToCSharpDomainMapper.BuildMappingSummary(domainMap));
        sb.AppendLine();
        sb.AppendLine("Key upstream files:");
        foreach (var snippet in digest.FileSnippets)
        {
            sb.AppendLine($"--- {snippet.Path} ---");
            sb.AppendLine(snippet.Excerpt);
            sb.AppendLine();
        }

        var text = sb.ToString();
        return text.Length <= maxChars ? text : text[..maxChars] + "\n…(truncated)";
    }

    private static UpstreamSemanticDigest ExtractDigest(IReadOnlyList<GeneratedFile> files)
    {
        var upstreamFiles = files
            .Where(f => f.RelativePath.Replace('\\', '/').StartsWith("upstream/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var exportedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var columnLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var concepts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var snippets = new List<UpstreamFileSnippet>();

        foreach (var file in upstreamFiles)
        {
            var content = file.Content ?? string.Empty;
            if (content.Length == 0)
                continue;

            foreach (Match m in ExportEnum.Matches(content))
                exportedTypes.Add(m.Groups[1].Value);
            foreach (Match m in ExportInterface.Matches(content))
                exportedTypes.Add(m.Groups[1].Value);
            foreach (Match m in ExportType.Matches(content))
                exportedTypes.Add(m.Groups[1].Value);

            foreach (Match m in ConstArray.Matches(content))
            {
                foreach (Match label in Regex.Matches(m.Groups[2].Value, @"['""]([^'""]{2,40})['""]"))
                    columnLabels.Add(label.Groups[1].Value);
            }

            if (content.Contains("kanban", StringComparison.OrdinalIgnoreCase))
                concepts.Add("kanban");
            if (content.Contains("board", StringComparison.OrdinalIgnoreCase))
                concepts.Add("board");
            if (content.Contains("column", StringComparison.OrdinalIgnoreCase))
                concepts.Add("column");
            if (content.Contains("card", StringComparison.OrdinalIgnoreCase))
                concepts.Add("card");
            if (content.Contains("task", StringComparison.OrdinalIgnoreCase))
                concepts.Add("task");
            if (content.Contains("lane", StringComparison.OrdinalIgnoreCase))
                concepts.Add("lane");

            if (snippets.Count < 6 && IsSourceLike(file.RelativePath))
            {
                snippets.Add(new UpstreamFileSnippet(
                    file.RelativePath.Replace('\\', '/'),
                    Truncate(content, 1200)));
            }
        }

        if (columnLabels.Count < 2)
        {
            columnLabels.Add("Backlog");
            columnLabels.Add("In Progress");
            columnLabels.Add("Done");
        }

        return new UpstreamSemanticDigest(
            upstreamFiles.Count,
            exportedTypes.OrderBy(x => x).ToList(),
            columnLabels.OrderBy(x => x).ToList(),
            concepts.OrderBy(x => x).ToList(),
            snippets);
    }

    private static bool IsSourceLike(string path)
    {
        var lower = path.ToLowerInvariant();
        return lower.EndsWith(".ts") || lower.EndsWith(".tsx") || lower.EndsWith(".js")
               || lower.EndsWith(".jsx") || lower.EndsWith(".md");
    }

    private static string BuildExtractMarkdown(UpstreamSemanticDigest digest, TypeScriptDomainMapResult domainMap)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Upstream semantic extract");
        sb.AppendLine();
        sb.AppendLine("This file is generated deterministically from the cloned `upstream/` snapshot.");
        sb.AppendLine();
        sb.AppendLine($"Upstream files scanned: **{digest.UpstreamFileCount}**");
        sb.AppendLine();
        sb.AppendLine("## Discovered concepts");
        foreach (var c in digest.Concepts)
            sb.AppendLine($"- {c}");
        sb.AppendLine();
        sb.AppendLine("## Exported types (heuristic)");
        foreach (var t in digest.ExportedTypes)
            sb.AppendLine($"- `{t}`");
        sb.AppendLine();
        sb.AppendLine("## TypeScript → C# mapped types");
        foreach (var en in domainMap.Enums)
            sb.AppendLine($"- enum `{en.Name}` from `{en.SourcePath}`");
        foreach (var rec in domainMap.Records)
            sb.AppendLine($"- record `{rec.Name}` from `{rec.SourcePath}`");
        sb.AppendLine();
        sb.AppendLine("## Column labels");
        foreach (var label in digest.ColumnLabels)
            sb.AppendLine($"- {label}");
        sb.AppendLine();
        sb.AppendLine("## Source snippets");
        foreach (var s in digest.FileSnippets)
        {
            sb.AppendLine($"### `{s.Path}`");
            sb.AppendLine("```");
            sb.AppendLine(s.Excerpt);
            sb.AppendLine("```");
        }

        return sb.ToString();
    }

    private static string BuildSemanticMapFile(string ns, UpstreamSemanticDigest digest, TypeScriptDomainMapResult domainMap)
    {
        var types = string.Join(", ", digest.ExportedTypes.Select(t => $"\"{t}\""));
        var mapped = string.Join(", ", domainMap.Enums.Select(e => $"\"{e.Name}\"").Concat(domainMap.Records.Select(r => $"\"{r.Name}\"")));
        var columns = string.Join(", ", digest.ColumnLabels.Select(c => $"\"{c}\""));
        var concepts = string.Join(", ", digest.Concepts.Select(c => $"\"{c}\""));
        var sources = string.Join(", ", digest.FileSnippets.Select(s => $"\"{s.Path}\""));

        return $@"// Semantic map adapted from upstream snapshot (deterministic extract).
namespace {ns}.Domain;

public static class UpstreamSemanticMap
{{
    public static readonly string[] SourceFiles = new[] {{ {sources} }};

    public static readonly string[] ExportedTypes = new[] {{ {types} }};

    public static readonly string[] MappedTypes = new[] {{ {mapped} }};

    public static readonly string[] ColumnLabels = new[] {{ {columns} }};

    public static readonly string[] Concepts = new[] {{ {concepts} }};
}}
";
    }

    private static string EnrichKanbanService(string existing, string ns, UpstreamSemanticDigest digest)
    {
        if (existing.Contains("UpstreamSemanticMap", StringComparison.Ordinal))
            return existing;

        var columnInit = string.Join(",\n        ", digest.ColumnLabels.Select(label =>
        {
            var id = Slug(label);
            return $"new KanbanColumn(\"{id}\", \"{label}\")";
        }));

        return $@"// Adapted from upstream semantic map ({string.Join(", ", digest.Concepts)})
using {ns}.Domain;

namespace {ns}.Services;

public sealed class KanbanBoardService
{{
    private readonly List<KanbanTask> _tasks = new()
    {{
        new KanbanTask(""task-1"", ""Card adapted from upstream semantics"", ""{Slug(digest.ColumnLabels.FirstOrDefault() ?? "backlog")}"")
    }};

    public IReadOnlyList<KanbanColumn> GetColumns() =>
        UpstreamSemanticMap.ColumnLabels
            .Select(label => new KanbanColumn(Slug(label), label))
            .ToList();

    public IReadOnlyList<KanbanTask> GetTasks() => _tasks;

    public KanbanTask? MoveTask(string taskId, string targetColumnId)
    {{
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null)
            return null;
        var idx = _tasks.FindIndex(t => t.Id == taskId);
        _tasks[idx] = task with {{ ColumnId = targetColumnId }};
        return _tasks[idx];
    }}

    private static string Slug(string title)
    {{
        var slug = System.Text.RegularExpressions.Regex.Replace(
            title.ToLowerInvariant(),
            ""[^a-z0-9]+"",
            ""_"",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant).Trim('_');
        return string.IsNullOrWhiteSpace(slug) ? ""column"" : slug;
    }}
}}
";
    }

    private static string Slug(string title)
    {
        var slug = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
        return string.IsNullOrWhiteSpace(slug) ? "column" : slug;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "\n…";

    private static string BuildNamespaceFromRoot(string root)
    {
        var normalized = root.Replace('\\', '/').Trim('/');
        var segments = normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .SkipWhile(s => s.Equals("src", StringComparison.OrdinalIgnoreCase))
            .Select(s => new string(s.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray()))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();
        return segments.Length == 0 ? "GeneratedApp.Api" : string.Join('.', segments);
    }

    private static int Upsert(IList<GeneratedFile> files, GeneratedFile patch)
    {
        for (var i = 0; i < files.Count; i++)
        {
            if (!files[i].RelativePath.Equals(patch.RelativePath, StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.Equals(files[i].Content, patch.Content, StringComparison.Ordinal))
                return 0;
            files[i] = patch;
            return 1;
        }

        files.Add(patch);
        return 1;
    }
}

public sealed record UpstreamSemanticDigest(
    int UpstreamFileCount,
    IReadOnlyList<string> ExportedTypes,
    IReadOnlyList<string> ColumnLabels,
    IReadOnlyList<string> Concepts,
    IReadOnlyList<UpstreamFileSnippet> FileSnippets);

public sealed record UpstreamFileSnippet(string Path, string Excerpt);
