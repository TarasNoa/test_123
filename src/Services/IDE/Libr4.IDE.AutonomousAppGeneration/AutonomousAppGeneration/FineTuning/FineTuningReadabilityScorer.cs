namespace Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;

public static class FineTuningReadabilityScorer
{
    public static double Score(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
            return 0;

        var words = text.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return 0;

        var avgWordLength = words.Average(w => w.Length);
        var avgLineLength = lines.Average(l => l.Length);
        var commentLines = lines.Count(l => l.TrimStart().StartsWith("//")
                                            || l.TrimStart().StartsWith('#')
                                            || l.TrimStart().StartsWith("/*"));
        var commentRatio = (double)commentLines / lines.Length;

        var lengthScore = Math.Clamp(avgWordLength / 8.0, 0, 1);
        var lineScore = Math.Clamp(1.0 - Math.Abs(avgLineLength - 60) / 120.0, 0, 1);
        var commentScore = Math.Clamp(commentRatio * 4, 0, 1);

        return Math.Clamp((lengthScore * 0.35) + (lineScore * 0.45) + (commentScore * 0.20), 0, 1);
    }
}
