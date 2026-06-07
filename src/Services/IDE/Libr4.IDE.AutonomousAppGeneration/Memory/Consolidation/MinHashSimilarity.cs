using System.Security.Cryptography;
using System.Text;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Consolidation;

public static class MinHashSimilarity
{
    private const int Bands = 32;

    public static int[] ComputeSignature(string text)
    {
        var shingles = BuildShingles(text);
        var signature = new int[Bands];
        if (shingles.Count == 0)
            return signature;

        for (var band = 0; band < Bands; band++)
        {
            var min = int.MaxValue;
            foreach (var shingle in shingles)
            {
                var hash = HashToInt(shingle, band);
                if (hash < min)
                    min = hash;
            }

            signature[band] = min == int.MaxValue ? 0 : min;
        }

        return signature;
    }

    public static double EstimateSimilarity(int[] left, int[] right)
    {
        if (left.Length == 0 || right.Length == 0 || left.Length != right.Length)
            return 0;

        var matches = 0;
        var compared = 0;
        for (var i = 0; i < left.Length; i++)
        {
            if (left[i] == 0 || right[i] == 0)
                continue;
            compared++;
            if (left[i] == right[i])
                matches++;
        }

        return compared == 0 ? 0 : (double)matches / compared;
    }

    public static double EstimateTextSimilarity(string left, string right)
    {
        var minHash = EstimateSimilarity(ComputeSignature(left), ComputeSignature(right));
        var jaccard = TokenJaccard(left, right);
        return Math.Max(minHash, jaccard);
    }

    private static double TokenJaccard(string left, string right)
    {
        var leftTokens = Tokenize(left);
        var rightTokens = Tokenize(right);
        if (leftTokens.Count == 0 || rightTokens.Count == 0)
            return 0;

        var intersection = leftTokens.Intersect(rightTokens).Count();
        var union = leftTokens.Union(rightTokens).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }

    private static HashSet<string> Tokenize(string text) =>
        text.ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', ',', '.', ';', ':', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 2)
            .ToHashSet(StringComparer.Ordinal);

    private static HashSet<string> BuildShingles(string text)
    {
        var tokens = text
            .ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', ',', '.', ';', ':', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 2)
            .ToArray();

        var shingles = new HashSet<string>(StringComparer.Ordinal);
        if (tokens.Length == 0)
        {
            if (!string.IsNullOrWhiteSpace(text))
                shingles.Add(text.ToLowerInvariant().Trim());
            return shingles;
        }

        if (tokens.Length == 1)
        {
            shingles.Add(tokens[0]);
            return shingles;
        }

        for (var i = 0; i < tokens.Length - 1; i++)
            shingles.Add($"{tokens[i]} {tokens[i + 1]}");

        return shingles;
    }

    private static int HashToInt(string value, int seed)
    {
        var payload = Encoding.UTF8.GetBytes($"{seed}:{value}");
        var hash = SHA256.HashData(payload);
        return BitConverter.ToInt32(hash, 0) & int.MaxValue;
    }
}
