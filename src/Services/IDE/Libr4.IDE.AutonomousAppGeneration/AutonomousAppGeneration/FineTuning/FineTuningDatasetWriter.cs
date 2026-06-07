using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FineTuning;

public sealed class FineTuningDatasetWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly FineTuningDataPipelineOptions _options;

    public FineTuningDatasetWriter(IOptions<FineTuningDataPipelineOptions> options) =>
        _options = options.Value;

    public async Task<string> AppendAsync(FineTuningExample example, CancellationToken ct = default)
    {
        var root = ResolveRoot();
        Directory.CreateDirectory(root);
        var stackDir = Path.Combine(root, example.Stack);
        Directory.CreateDirectory(stackDir);
        var path = Path.Combine(stackDir, "train.jsonl");

        var line = JsonSerializer.Serialize(new
        {
            instruction = example.Instruction,
            output = example.Output,
            metadata = new
            {
                runId = example.RunId,
                stack = example.Stack,
                createdAtUtc = example.CreatedAtUtc
            }
        }, JsonOptions);

        await File.AppendAllTextAsync(path, line + Environment.NewLine, ct).ConfigureAwait(false);
        return path;
    }

    private string ResolveRoot() =>
        Path.IsPathRooted(_options.DatasetsRoot)
            ? _options.DatasetsRoot
            : Path.GetFullPath(_options.DatasetsRoot);
}
