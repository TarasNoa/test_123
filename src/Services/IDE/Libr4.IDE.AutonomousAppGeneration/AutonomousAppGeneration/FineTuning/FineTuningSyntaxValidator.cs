namespace Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;

public static class FineTuningSyntaxValidator
{
    public static bool ValidateFile(string relativePath, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        var ext = Path.GetExtension(relativePath).ToLowerInvariant();
        return ext switch
        {
            ".cs" => BalancedBraces(content, '{', '}'),
            ".js" or ".jsx" or ".ts" or ".tsx" => BalancedBraces(content, '{', '}') && BalancedAngle(content),
            ".py" => !content.Contains("\0"),
            ".json" => TryParseJson(content),
            ".yaml" or ".yml" => content.Split('\n').All(line => !line.TrimStart().StartsWith('\t')),
            _ => content.Length >= 8
        };
    }

    public static bool ValidateBundle(IEnumerable<(string Path, string Content)> files)
    {
        var codeFiles = files
            .Where(f => IsCodePath(f.Path))
            .ToList();
        if (codeFiles.Count == 0)
            return false;

        return codeFiles.All(f => ValidateFile(f.Path, f.Content));
    }

    private static bool IsCodePath(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".cs" or ".py" or ".js" or ".jsx" or ".ts" or ".tsx" or ".json";
    }

    private static bool BalancedBraces(string content, char open, char close)
    {
        var depth = 0;
        foreach (var ch in content)
        {
            if (ch == open) depth++;
            if (ch == close) depth--;
            if (depth < 0) return false;
        }

        return depth == 0;
    }

    private static bool BalancedAngle(string content)
    {
        var depth = 0;
        foreach (var ch in content)
        {
            if (ch == '<') depth++;
            if (ch == '>') depth--;
            if (depth < 0) return false;
        }

        return depth >= 0;
    }

    private static bool TryParseJson(string content)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(content);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
