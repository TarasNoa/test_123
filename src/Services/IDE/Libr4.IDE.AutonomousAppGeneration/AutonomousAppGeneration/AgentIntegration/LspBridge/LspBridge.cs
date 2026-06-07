using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.LspBridge;

/// <summary>
/// LSP bridge: process-backed language servers when configured, compiler errors as fallback diagnostics.
/// </summary>
public sealed class LspBridge : ILspBridge
{
    private readonly LspBridgeOptions _options;
    private readonly ProcessLspClient _processClient;
    private readonly ILogger<LspBridge> _logger;

    public LspBridge(
        IOptions<LspBridgeOptions> options,
        ProcessLspClient processClient,
        ILogger<LspBridge> logger)
    {
        _options = options.Value;
        _processClient = processClient;
        _logger = logger;
    }

    public async Task<LspWorkspaceContext> GetWorkspaceContextAsync(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan? plan,
        IReadOnlyList<ErrorReport>? compilerErrors,
        IReadOnlyList<string>? focusPaths,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return LspWorkspaceContext.Empty;

        var diagnostics = new List<LspDiagnostic>();
        var definitions = new List<LspLocation>();
        var references = new List<LspLocation>();

        if (compilerErrors is { Count: > 0 })
            diagnostics.AddRange(MapCompilerErrors(compilerErrors));

        if (_options.EnableProcessServers && files.Count > 0)
        {
            var targets = ResolveTargetFiles(files, focusPaths).Take(3).ToList();
            foreach (var file in targets)
            {
                var profileKey = LspStackProfileResolver.ResolveProfileKey(plan, file.RelativePath);
                if (profileKey is null || !_options.Servers.ContainsKey(profileKey))
                    continue;

                try
                {
                    var fromServer = await _processClient.GetDiagnosticsAsync(
                            profileKey,
                            file,
                            ct)
                        .ConfigureAwait(false);
                    diagnostics.AddRange(fromServer);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "LSP diagnostics unavailable for {Path} via {Profile}", file.RelativePath, profileKey);
                }
            }
        }

        diagnostics = diagnostics
            .GroupBy(d => $"{d.FilePath}:{d.Line}:{d.Message}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(_options.MaxDiagnosticsPerFile * 4)
            .ToList();

        return new LspWorkspaceContext(diagnostics, definitions, references);
    }

    private static IEnumerable<GeneratedFile> ResolveTargetFiles(
        IReadOnlyList<GeneratedFile> files,
        IReadOnlyList<string>? focusPaths)
    {
        if (focusPaths is { Count: > 0 })
        {
            foreach (var path in focusPaths)
            {
                var match = files.FirstOrDefault(f =>
                    f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase)
                    || f.RelativePath.Replace('\\', '/').EndsWith(path.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    yield return match;
            }

            yield break;
        }

        foreach (var file in files.Take(5))
            yield return file;
    }

    private static IEnumerable<LspDiagnostic> MapCompilerErrors(IReadOnlyList<ErrorReport> errors) =>
        errors.Select(e => new LspDiagnostic(
            e.FilePath ?? "(unknown)",
            e.LineNumber ?? 0,
            0,
            "error",
            e.Message,
            e.ErrorType,
            "compiler"));
}
