using System.Text;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Builds a deterministic bridge document linking the cloned upstream snapshot to the
/// generated product stack so planners/fixers and quality gates see explicit adaptation intent.
/// </summary>
public static class UpstreamAdaptationBridgeBuilder
{
    public static int TryAppendBridgeDocument(IList<GeneratedFile> files, GenerationPlan plan)
    {
        var upstreamPaths = files
            .Select(f => f.RelativePath.Replace('\\', '/').TrimStart('/'))
            .Where(p => p.StartsWith("upstream/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (upstreamPaths.Count == 0)
            return 0;

        RepoBootstrapProbe probe = default;
        var hasProbe = false;
        var evidence = files.FirstOrDefault(f =>
            f.RelativePath.Equals("BOOTSTRAP_EVIDENCE.md", StringComparison.OrdinalIgnoreCase));
        if (evidence?.Content is not null)
            hasProbe = RepoBootstrapDetailsParser.TryParse(evidence.Content, out probe);

        var readme = files.FirstOrDefault(f =>
        {
            var p = f.RelativePath.Replace('\\', '/');
            return p.Equals("upstream/README.md", StringComparison.OrdinalIgnoreCase)
                   || p.EndsWith("/README.md", StringComparison.OrdinalIgnoreCase);
        });

        var sb = new StringBuilder();
        sb.AppendLine("# Upstream adaptation bridge");
        sb.AppendLine();
        sb.AppendLine("This document connects the materialized upstream snapshot (`upstream/`) to the generated product code.");
        sb.AppendLine();
        if (hasProbe)
        {
            sb.AppendLine("## Source");
            sb.AppendLine($"- Repository: `{probe.Repository ?? "unknown"}`");
            sb.AppendLine($"- Clone URL: `{probe.CloneUrl}`");
            if (!string.IsNullOrWhiteSpace(probe.License))
                sb.AppendLine($"- License: **{probe.License}**");
            sb.AppendLine();
        }

        sb.AppendLine("## Upstream snapshot index");
        sb.AppendLine($"Total files under `upstream/`: **{upstreamPaths.Count}**");
        sb.AppendLine();
        foreach (var path in upstreamPaths.Take(24))
            sb.AppendLine($"- `{path}`");
        if (upstreamPaths.Count > 24)
            sb.AppendLine($"- … and {upstreamPaths.Count - 24} more");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(readme?.Content))
        {
            sb.AppendLine("## Upstream README excerpt");
            sb.AppendLine();
            var excerpt = readme.Content.Length > 2000
                ? readme.Content[..2000] + "\n\n…"
                : readme.Content;
            sb.AppendLine(excerpt.Trim());
            sb.AppendLine();
        }

        sb.AppendLine("## Required product integration (not optional)");
        sb.AppendLine();
        sb.AppendLine("1. Preserve upstream license and attribution in `BOOTSTRAP_EVIDENCE.md` and runtime docs.");
        sb.AppendLine("2. Map upstream kanban/board/column/task concepts into API controllers and domain models.");
        sb.AppendLine("3. Add JWT auth (`AuthController`, `AddJwtBearer`) protecting kanban/task mutations.");
        sb.AppendLine("4. Add business tests covering token issuance and column transitions.");
        sb.AppendLine();

        var productRoot = files
            .Select(f => f.RelativePath.Replace('\\', '/'))
            .FirstOrDefault(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            ?.Replace('\\', '/');
        productRoot = productRoot is null
            ? "src/GeneratedApp.Api"
            : productRoot[..productRoot.LastIndexOf('/')];

        sb.AppendLine("## Target integration points");
        sb.AppendLine($"- Product API root: `{productRoot}`");
        sb.AppendLine($"- Planned stack: {string.Join(", ", plan.TechStack?.Languages ?? Array.Empty<string>())} / {string.Join(", ", plan.TechStack?.Frameworks ?? Array.Empty<string>())}");
        sb.AppendLine($"- Build: `{string.Join(" ; ", plan.BuildCommands ?? Array.Empty<string>())}`");
        sb.AppendLine();

        var content = sb.ToString();
        var idx = -1;
        for (var i = 0; i < files.Count; i++)
        {
            if (!files[i].RelativePath.Equals("ADAPTATION_BRIDGE.md", StringComparison.OrdinalIgnoreCase))
                continue;
            idx = i;
            break;
        }

        var bridge = new GeneratedFile("ADAPTATION_BRIDGE.md", "markdown", content);
        if (idx < 0)
        {
            files.Add(bridge);
            return 1;
        }

        if (string.Equals(files[idx].Content, content, StringComparison.Ordinal))
            return 0;

        files[idx] = bridge;
        return 1;
    }
}
