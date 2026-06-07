using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Consolidation;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;

public sealed class FineTuningQualityFilter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly FineTuningDataPipelineOptions _options;
    private readonly List<int[]> _signatures = new();
    private readonly object _lock = new();

    public FineTuningQualityFilter(IOptions<FineTuningDataPipelineOptions> options)
    {
        _options = options.Value;
        LoadSignatures();
    }

    public FineTuningQualityReport Evaluate(FineTuningExample example, IEnumerable<(string Path, string Content)> files)
    {
        if (example.Output.Length < _options.MinOutputChars)
            return Reject(example, 0, syntaxValid: false, duplicate: false, "output_too_short");

        if (example.Output.Length > _options.MaxOutputChars)
            return Reject(example, 0, syntaxValid: false, duplicate: false, "output_too_long");

        var syntaxValid = FineTuningSyntaxValidator.ValidateBundle(files);
        if (!syntaxValid)
            return Reject(example, 0, false, false, "syntax_invalid");

        var readability = FineTuningReadabilityScorer.Score(example.Output);
        if (readability < _options.MinReadabilityScore)
            return Reject(example, readability, syntaxValid, false, "readability_low");

        var signature = MinHashSimilarity.ComputeSignature($"{example.Instruction}\n{example.Output}");
        lock (_lock)
        {
            foreach (var existing in _signatures)
            {
                if (MinHashSimilarity.EstimateSimilarity(existing, signature) >= _options.MinHashDedupThreshold)
                    return Reject(example, readability, syntaxValid, true, "duplicate_minhash");
            }

            _signatures.Add(signature);
            PersistSignature(example, signature);
        }

        return new FineTuningQualityReport(true, readability, syntaxValid, false, null);
    }

    private FineTuningQualityReport Reject(
        FineTuningExample example,
        double readability,
        bool syntaxValid,
        bool duplicate,
        string reason) =>
        new(false, readability, syntaxValid, duplicate, reason);

    private void LoadSignatures()
    {
        var path = ResolvePath(_options.SignaturesIndexPath);
        if (!File.Exists(path))
            return;

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            try
            {
                var entry = JsonSerializer.Deserialize<SignatureIndexEntry>(line, JsonOptions);
                if (entry?.Signature is { Length: > 0 })
                    _signatures.Add(entry.Signature);
            }
            catch
            {
                // skip corrupt lines
            }
        }
    }

    private void PersistSignature(FineTuningExample example, int[] signature)
    {
        var path = ResolvePath(_options.SignaturesIndexPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var entry = new SignatureIndexEntry(example.RunId, example.Stack, signature, DateTime.UtcNow);
        File.AppendAllText(path, JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine);
    }

    private static string ResolvePath(string configured) =>
        Path.IsPathRooted(configured) ? configured : Path.GetFullPath(configured);

    private sealed record SignatureIndexEntry(Guid RunId, string Stack, int[] Signature, DateTime CreatedAtUtc);
}
