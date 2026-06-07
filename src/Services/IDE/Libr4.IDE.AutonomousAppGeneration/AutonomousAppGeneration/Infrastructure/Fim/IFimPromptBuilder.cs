using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Fim;

public interface IFimPromptBuilder
{
    bool TryBuild(
        string relativePath,
        string fileContent,
        int? errorLine,
        int holeRadiusLines,
        out FimPrompt prompt);

    string FormatLlmPrompt(FimPrompt prompt);

    bool TryParseFill(string raw, out string fill);

    bool TryApplyFill(string originalContent, FimPrompt prompt, string fill, out string patched);

    FimGenerationContext ToGenerationContext(FimPrompt prompt);

    bool ShouldUseFim(GeneratedFile file, ErrorReport? error, int minFileLines);
}

public sealed class FimPromptBuilder : IFimPromptBuilder
{
    public const string HoleMarker = "<|fim_hole|>";

    public bool TryBuild(
        string relativePath,
        string fileContent,
        int? errorLine,
        int holeRadiusLines,
        out FimPrompt prompt)
    {
        prompt = null!;
        if (string.IsNullOrWhiteSpace(fileContent) || errorLine is not int line || line < 1)
            return false;

        var lines = fileContent.Replace("\r\n", "\n").Split('\n');
        if (line > lines.Length)
            return false;

        var radius = Math.Clamp(holeRadiusLines, 1, 32);
        var holeStart = Math.Max(1, line - radius);
        var holeEnd = Math.Min(lines.Length, line + radius);
        var prefix = string.Join('\n', lines.Take(holeStart - 1));
        var hole = string.Join('\n', lines.Skip(holeStart - 1).Take(holeEnd - holeStart + 1));
        var suffix = string.Join('\n', lines.Skip(holeEnd));

        if (string.IsNullOrEmpty(hole))
            return false;

        prompt = new FimPrompt(
            relativePath.Replace('\\', '/'),
            prefix,
            suffix,
            hole,
            holeStart,
            holeEnd);
        return true;
    }

    public string FormatLlmPrompt(FimPrompt prompt)
    {
        var body = string.IsNullOrEmpty(prompt.Prefix)
            ? HoleMarker
            : string.IsNullOrEmpty(prompt.Suffix)
                ? $"{prompt.Prefix}\n{HoleMarker}"
                : $"{prompt.Prefix}\n{HoleMarker}\n{prompt.Suffix}";
        return $"# {prompt.RelativePath}\n{body}";
    }

    public bool TryParseFill(string raw, out string fill)
    {
        fill = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var trimmed = raw.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0)
            {
                var endFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (endFence > firstNewline)
                    trimmed = trimmed[(firstNewline + 1)..endFence].Trim();
            }
        }

        if (trimmed.Contains(HoleMarker, StringComparison.Ordinal))
            return false;

        fill = trimmed;
        return fill.Length > 0;
    }

    public bool TryApplyFill(string originalContent, FimPrompt prompt, string fill, out string patched)
    {
        patched = originalContent;
        if (string.IsNullOrEmpty(fill))
            return false;

        var normalized = originalContent.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        if (prompt.HoleStartLine < 1 || prompt.HoleEndLine > lines.Length)
            return false;

        var before = lines.Take(prompt.HoleStartLine - 1);
        var after = lines.Skip(prompt.HoleEndLine);
        var fillLines = fill.Replace("\r\n", "\n").Split('\n');
        patched = string.Join('\n', before.Concat(fillLines).Concat(after));
        return !string.Equals(normalized, patched, StringComparison.Ordinal);
    }

    public FimGenerationContext ToGenerationContext(FimPrompt prompt) =>
        new(
            prompt.RelativePath,
            prompt.Prefix,
            prompt.Suffix,
            prompt.HoleContent,
            prompt.HoleStartLine,
            prompt.HoleEndLine);

    public bool ShouldUseFim(GeneratedFile file, ErrorReport? error, int minFileLines)
    {
        if (error?.LineNumber is not int line || line < 1)
            return false;
        if (string.IsNullOrWhiteSpace(file.Content))
            return false;

        var lineCount = file.Content.Replace("\r\n", "\n").Split('\n').Length;
        return lineCount >= Math.Max(1, minFileLines);
    }
}

public static class FimOutputApplier
{
    public static IReadOnlyList<GeneratedFile> ApplyOrFallback(
        IReadOnlyList<GeneratedFile> currentFiles,
        FimPrompt prompt,
        string fill,
        IFimPromptBuilder builder)
    {
        var file = currentFiles.FirstOrDefault(f =>
            f.RelativePath.Equals(prompt.RelativePath, StringComparison.OrdinalIgnoreCase));
        if (file is null)
            return Array.Empty<GeneratedFile>();

        if (builder.TryApplyFill(file.Content ?? string.Empty, prompt, fill, out var patched))
            return [new GeneratedFile(file.RelativePath, file.Language, patched)];

        var result = SurgicalPatchEngine.Apply(
            currentFiles,
            [new SurgicalPatchEngine.SurgicalEdit(prompt.RelativePath, prompt.HoleContent, fill)]);
        return result.Patches;
    }
}
