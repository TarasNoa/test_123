using System.Text.RegularExpressions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.ExecPolicy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.ExecPolicy;

public interface IExecPolicyEngine
{
    ExecPolicyEvaluation EvaluateBash(string command);
    void Audit(ExecPolicyAuditEntry entry);
    IReadOnlyList<ExecPolicyAuditEntry> GetAudit(Guid? runId);
}

public sealed class YamlExecPolicyEngine : IExecPolicyEngine
{
    private readonly IReadOnlyList<ExecPolicyRule> _rules;
    private readonly List<ExecPolicyAuditEntry> _audit = new();
    private readonly object _lock = new();
    private readonly ILogger<YamlExecPolicyEngine> _logger;

    public YamlExecPolicyEngine(IOptions<AgentRuntimeOptions> options, ILogger<YamlExecPolicyEngine> logger)
    {
        _logger = logger;
        _rules = LoadRules(options.Value.ExecPolicyPath);
    }

    public ExecPolicyEvaluation EvaluateBash(string command)
    {
        var normalized = command.Trim();
        foreach (var rule in _rules.Where(r => r.Action.Equals("bash", StringComparison.OrdinalIgnoreCase)))
        {
            if (Matches(normalized, rule.Pattern))
                return new ExecPolicyEvaluation(rule.Decision, rule.Pattern, $"matched rule: {rule.Pattern}");
        }

        return new ExecPolicyEvaluation(ExecPolicyDecision.Allow, null, null);
    }

    public void Audit(ExecPolicyAuditEntry entry)
    {
        lock (_lock)
        {
            _audit.Add(entry);
        }
    }

    public IReadOnlyList<ExecPolicyAuditEntry> GetAudit(Guid? runId) =>
        _audit.Where(a => a.RunId == runId).ToList();

    private static IReadOnlyList<ExecPolicyRule> LoadRules(string path)
    {
        if (!File.Exists(path))
            return DefaultRules();

        try
        {
            var yaml = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
            var doc = deserializer.Deserialize<ExecPolicyDocument>(yaml);
            return doc?.Rules?.Select(r => new ExecPolicyRule(
                r.Action ?? "bash",
                r.Pattern ?? "*",
                Enum.TryParse<ExecPolicyDecision>(r.Decision, true, out var d) ? d : ExecPolicyDecision.Prompt)).ToList()
                ?? DefaultRules();
        }
        catch
        {
            return DefaultRules();
        }
    }

    private static IReadOnlyList<ExecPolicyRule> DefaultRules() =>
    [
        new("bash", "rm -rf*", ExecPolicyDecision.Forbid),
        new("bash", "curl*|bash", ExecPolicyDecision.Forbid),
        new("bash", "wget*|bash", ExecPolicyDecision.Forbid),
        new("bash", "mvn*", ExecPolicyDecision.Allow),
        new("bash", "npm*", ExecPolicyDecision.Allow),
        new("bash", "pip*", ExecPolicyDecision.Allow),
        new("bash", "python manage.py*", ExecPolicyDecision.Allow),
    ];

    private static bool Matches(string command, string pattern)
    {
        if (pattern == "*")
            return true;
        if (pattern.EndsWith('*'))
            return command.StartsWith(pattern.TrimEnd('*'), StringComparison.OrdinalIgnoreCase);
        return Regex.IsMatch(command, WildcardToRegex(pattern), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string WildcardToRegex(string pattern) =>
        "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";

    private sealed class ExecPolicyDocument
    {
        public List<ExecPolicyRuleYaml>? Rules { get; set; }
    }

    private sealed class ExecPolicyRuleYaml
    {
        public string? Action { get; set; }
        public string? Pattern { get; set; }
        public string? Decision { get; set; }
    }
}
