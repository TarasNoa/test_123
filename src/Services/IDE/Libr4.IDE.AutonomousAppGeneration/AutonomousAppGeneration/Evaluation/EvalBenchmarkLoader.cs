using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Evaluation;

public static class EvalBenchmarkLoader
{
    public static EvalBenchmarkDefinition LoadFromFile(string path)
    {
        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        var doc = deserializer.Deserialize<EvalBenchmarkDefinition>(yaml)
                  ?? throw new InvalidOperationException($"empty eval benchmark: {path}");
        if (string.IsNullOrWhiteSpace(doc.Id))
            doc.Id = Path.GetFileNameWithoutExtension(path).Replace(".eval", string.Empty);
        return doc;
    }

    public static IReadOnlyList<EvalBenchmarkDefinition> LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return Array.Empty<EvalBenchmarkDefinition>();

        return Directory.EnumerateFiles(directory, "*.eval.yaml", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(LoadFromFile)
            .ToList();
    }
}

public sealed class EvalBenchmarkCatalog
{
    private readonly InternalEvalOptions _options;
    private IReadOnlyList<EvalBenchmarkDefinition>? _cache;

    public EvalBenchmarkCatalog(IOptions<InternalEvalOptions> options) => _options = options.Value;

    public IReadOnlyList<EvalBenchmarkDefinition> All
    {
        get
        {
            _cache ??= EvalBenchmarkLoader.LoadDirectory(BenchmarksRoot);
            return _cache;
        }
    }

    public string BenchmarksRoot
    {
        get
        {
            var root = ResolveRoot(_options.EvaluationRoot);
            return Path.Combine(root, "benchmarks");
        }
    }

    public string BaselinePath
    {
        get
        {
            if (Path.IsPathRooted(_options.BaselineScoresPath))
                return _options.BaselineScoresPath;
            return Path.Combine(ResolveRoot(_options.EvaluationRoot), "baselines", "scores.json");
        }
    }

    private static string ResolveRoot(string configured)
    {
        if (Path.IsPathRooted(configured))
            return configured;

        var fromBase = Path.Combine(AppContext.BaseDirectory, configured);
        if (Directory.Exists(fromBase))
            return fromBase;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, configured);
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return Path.GetFullPath(configured);
    }
}
