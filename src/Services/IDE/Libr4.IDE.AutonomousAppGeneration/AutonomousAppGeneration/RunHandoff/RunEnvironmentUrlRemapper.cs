using System.Text.RegularExpressions;
using Libr4.IDE.Application.Obscura;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed class RunEnvironmentUrlRemapper
{
    private static readonly Regex LocalhostUrl = new(
        @"https?://(?:localhost|127\.0\.0\.1)(?::\d+)?[^\s""']*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IObscuraNetworkRouter? _networkRouter;

    public RunEnvironmentUrlRemapper(IObscuraNetworkRouter? networkRouter = null) =>
        _networkRouter = networkRouter;

    public async Task RemapRunArtifactsAsync(Guid runId, string runArtifactsDir, CancellationToken ct = default)
    {
        if (!Directory.Exists(runArtifactsDir))
            return;

        foreach (var file in Directory.EnumerateFiles(runArtifactsDir, "*.*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                && !file.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)
                && !file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ct.ThrowIfCancellationRequested();
            var text = await File.ReadAllTextAsync(file, ct).ConfigureAwait(false);
            if (!text.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                && !text.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var remapped = LocalhostUrl.Replace(text, match => RemapUrl(runId, match.Value));
            if (!string.Equals(text, remapped, StringComparison.Ordinal))
                await File.WriteAllTextAsync(file, remapped, ct).ConfigureAwait(false);
        }
    }

    private string RemapUrl(Guid runId, string url) =>
        _networkRouter?.ResolveForBrowser(runId, url) ?? url;
}
