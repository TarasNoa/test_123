namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Search;

internal static class FtsQueryHelper
{
    public static string ToMatchExpression(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "\"\"";

        var terms = query
            .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(EscapeTerm)
            .Where(term => term.Length > 0)
            .ToArray();

        return terms.Length == 0 ? "\"\"" : string.Join(" AND ", terms);
    }

    private static string EscapeTerm(string term)
    {
        var cleaned = term.Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{cleaned}\"";
    }
}
