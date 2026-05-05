using System.Text;
using System.Text.Json;
using Libr4.IDE.Application.CodeSearch;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Infrastructure.SemanticIndex;

/// <summary>
/// Context artifact service — gives agents structured knowledge beyond source code:
/// DB schemas, API specs, architecture docs, infra configs.
/// Analogous to SocratiCode context-artifacts.ts
/// </summary>
public interface ICodeContextArtifactService
{
    Task<string> BuildContextPackAsync(string projectPath, string agentQuery, ContextPackRequest request, CancellationToken ct = default);
    Task<string[]> ListArtifactsAsync(string projectPath, CancellationToken ct = default);
}

public sealed record ContextPackRequest(
    bool IncludeRelevantCode = true,
    bool IncludeSymbolGraph = true,
    bool IncludeArchitectureDoc = false,
    bool IncludeApiSpecs = false,
    int MaxCodeChunks = 8,
    int MaxContextTokens = 6000);

public sealed class CodeContextArtifactService : ICodeContextArtifactService
{
    private readonly ISemanticCodeIndex _index;
    private readonly ILogger<CodeContextArtifactService> _logger;

    public CodeContextArtifactService(ISemanticCodeIndex index, ILogger<CodeContextArtifactService> logger)
    {
        _index = index;
        _logger = logger;
    }

    public async Task<string> BuildContextPackAsync(
        string projectPath,
        string agentQuery,
        ContextPackRequest request,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Codebase Context Pack");
        sb.AppendLine($"Query: {agentQuery}");
        sb.AppendLine($"Project: {projectPath}");
        sb.AppendLine($"Generated: {DateTimeOffset.UtcNow:O}");
        sb.AppendLine();

        var tokenBudget = request.MaxContextTokens;

        // 1. Relevant code chunks via hybrid search
        if (request.IncludeRelevantCode)
        {
            var results = await _index.SearchAsync(projectPath, agentQuery,
                new CodeSearchOptions(TopK: request.MaxCodeChunks, MinScore: 0.1), ct);

            if (results.Length > 0)
            {
                sb.AppendLine("## Relevant Code");
                foreach (var r in results)
                {
                    var relPath = TryRelativize(projectPath, r.FilePath);
                    sb.AppendLine($"### {relPath}:{r.StartLine}-{r.EndLine}");
                    if (!string.IsNullOrWhiteSpace(r.SymbolName))
                        sb.AppendLine($"Symbol: `{r.SymbolName}` ({r.SymbolKind})");
                    sb.AppendLine($"```{r.Language}");
                    sb.AppendLine(TruncateToTokens(r.Content, tokenBudget / results.Length));
                    sb.AppendLine("```");
                    sb.AppendLine();
                }
            }
        }

        // 2. Symbol graph summary
        if (request.IncludeSymbolGraph)
        {
            var stats = await _index.GetGraphStatsAsync(projectPath, ct);
            sb.AppendLine("## Codebase Structure");
            sb.AppendLine($"- Files: {stats.TotalFiles}");
            sb.AppendLine($"- Symbols: {stats.TotalSymbols}");
            sb.AppendLine($"- Dependencies: {stats.TotalImports}");

            if (stats.LanguageBreakdown.Count > 0)
            {
                sb.AppendLine("- Languages:");
                foreach (var (lang, count) in stats.LanguageBreakdown.OrderByDescending(kv => kv.Value).Take(5))
                    sb.AppendLine($"  - {lang}: {count} symbols");
            }

            if (stats.MostConnectedFiles.Length > 0)
            {
                sb.AppendLine("- Most connected files (top 5):");
                foreach (var (file, connections) in stats.MostConnectedFiles.Take(5))
                    sb.AppendLine($"  - {TryRelativize(projectPath, file)} ({connections} connections)");
            }

            sb.AppendLine();
        }

        // 3. Load .socraticodeignore-equivalent artifact configs
        var artifactConfigPath = Path.Combine(projectPath, ".libr4contextartifacts.json");
        if (request.IncludeArchitectureDoc && File.Exists(artifactConfigPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(artifactConfigPath, ct);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("artifacts", out var artifacts))
                {
                    sb.AppendLine("## Project Artifacts");
                    foreach (var artifact in artifacts.EnumerateArray())
                    {
                        if (!artifact.TryGetProperty("path", out var pathEl)) continue;
                        var artPath = Path.Combine(projectPath, pathEl.GetString() ?? "");
                        if (!File.Exists(artPath)) continue;

                        var artContent = await File.ReadAllTextAsync(artPath, ct);
                        var name = artifact.TryGetProperty("name", out var n) ? n.GetString() : Path.GetFileName(artPath);
                        sb.AppendLine($"### {name}");
                        sb.AppendLine(TruncateToTokens(artContent, 800));
                        sb.AppendLine();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to load context artifacts config");
            }
        }

        return sb.ToString();
    }

    public async Task<string[]> ListArtifactsAsync(string projectPath, CancellationToken ct = default)
    {
        var status = await _index.GetStatusAsync(projectPath, ct);
        return
        [
            $"index:status={status.State}",
            $"index:files={status.TotalFiles}",
            $"index:chunks={status.ChunkCount}",
            $"index:symbols={status.SymbolCount}",
        ];
    }

    private static string TryRelativize(string basePath, string fullPath)
    {
        try { return Path.GetRelativePath(basePath, fullPath); }
        catch { return fullPath; }
    }

    private static string TruncateToTokens(string content, int maxTokens)
    {
        var approxChars = maxTokens * 4;
        return content.Length <= approxChars ? content : content[..approxChars] + "\n[... truncated]";
    }
}
