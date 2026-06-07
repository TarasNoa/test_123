using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;

/// <summary>
/// Keeps security remediation LLM prompts small: only files tied to findings plus minimal related config.
/// </summary>
public static class SecurityRemediationContextPolicy
{
    public const int DefaultMaxFiles = 10;
    public const int DefaultMaxCharsPerFile = 3500;

    public static IReadOnlyList<GeneratedFile> BuildContext(
        IReadOnlyList<GeneratedFile> currentFiles,
        IReadOnlyList<ErrorReport> errors,
        int maxFiles = DefaultMaxFiles,
        int maxCharsPerFile = DefaultMaxCharsPerFile)
    {
        var selected = new Dictionary<string, GeneratedFile>(StringComparer.OrdinalIgnoreCase);

        foreach (var err in errors)
        {
            if (string.IsNullOrWhiteSpace(err.FilePath))
                continue;

            var match = FindFile(currentFiles, err.FilePath);
            if (match is not null)
                selected[match.RelativePath] = TruncateFile(match, maxCharsPerFile);
        }

        var signal = string.Join(' ', errors.Select(e => $"{e.Message} {e.SuggestedFix}"));

        if (signal.Contains("transfer", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("race", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("idempotency", StringComparison.OrdinalIgnoreCase))
        {
            AddIfExists(currentFiles, selected, maxCharsPerFile, f =>
                f.RelativePath.Contains("TransferService", StringComparison.OrdinalIgnoreCase));
        }

        if (signal.Contains("jwt", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("credential", StringComparison.OrdinalIgnoreCase))
        {
            AddIfExists(currentFiles, selected, maxCharsPerFile, f =>
                f.RelativePath.Contains("application", StringComparison.OrdinalIgnoreCase)
                && (f.RelativePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                    || f.RelativePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                    || f.RelativePath.EndsWith(".properties", StringComparison.OrdinalIgnoreCase)));
            AddIfExists(currentFiles, selected, maxCharsPerFile, f =>
                f.RelativePath.Contains("SecurityConfig", StringComparison.OrdinalIgnoreCase)
                || f.RelativePath.Contains("/security/", StringComparison.OrdinalIgnoreCase));
        }

        if (signal.Contains("h2", StringComparison.OrdinalIgnoreCase)
            || signal.Contains("console", StringComparison.OrdinalIgnoreCase))
        {
            AddIfExists(currentFiles, selected, maxCharsPerFile, f =>
                f.RelativePath.Contains("application", StringComparison.OrdinalIgnoreCase));
        }

        AddIfExists(currentFiles, selected, maxCharsPerFile, f =>
            f.RelativePath.Equals("backend/pom.xml", StringComparison.OrdinalIgnoreCase));

        return selected.Values.Take(maxFiles).ToList();
    }

    private static void AddIfExists(
        IReadOnlyList<GeneratedFile> files,
        IDictionary<string, GeneratedFile> selected,
        int maxChars,
        Func<GeneratedFile, bool> predicate)
    {
        foreach (var file in files.Where(predicate))
            selected[file.RelativePath] = TruncateFile(file, maxChars);
    }

    private static GeneratedFile? FindFile(IReadOnlyList<GeneratedFile> files, string path)
    {
        var normalized = path.Replace('\\', '/').Trim();
        return files.FirstOrDefault(f =>
            f.RelativePath.Equals(normalized, StringComparison.OrdinalIgnoreCase)
            || f.RelativePath.EndsWith("/" + normalized, StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(f.RelativePath, StringComparison.OrdinalIgnoreCase));
    }

    private static GeneratedFile TruncateFile(GeneratedFile file, int maxChars)
    {
        if (string.IsNullOrEmpty(file.Content) || file.Content.Length <= maxChars)
            return file;

        return new GeneratedFile(
            file.RelativePath,
            file.Language,
            file.Content[..maxChars] + "\n// ... truncated for security remediation context ...\n");
    }
}
