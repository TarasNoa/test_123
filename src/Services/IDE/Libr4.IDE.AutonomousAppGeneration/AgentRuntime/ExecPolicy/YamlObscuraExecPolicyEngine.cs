using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.ExecPolicy;

public interface IObscuraExecPolicyEngine
{
    ObscuraExecPolicyEvaluation Evaluate(string toolName, string? target);
    void Audit(ObscuraExecPolicyAuditEntry entry);
    IReadOnlyList<ObscuraExecPolicyAuditEntry> GetAudit(Guid? runId);
}

public sealed class YamlObscuraExecPolicyEngine : IObscuraExecPolicyEngine
{
    private readonly ExecPolicyDecision _defaultDecision;
    private readonly IReadOnlyList<ObscuraExecPolicyRule> _urlRules;
    private readonly IReadOnlyList<ObscuraExecPolicyRule> _scriptRules;
    private readonly List<ObscuraExecPolicyAuditEntry> _audit = new();
    private readonly object _lock = new();
    private readonly ILogger<YamlObscuraExecPolicyEngine> _logger;

    public YamlObscuraExecPolicyEngine(
        IOptions<AgentRuntimeOptions> options,
        ILogger<YamlObscuraExecPolicyEngine> logger)
    {
        _logger = logger;
        var (defaultDecision, urlRules, scriptRules) = LoadRules(options.Value.ObscuraExecPolicyPath);
        _defaultDecision = defaultDecision;
        _urlRules = urlRules;
        _scriptRules = scriptRules;
    }

    public ObscuraExecPolicyEvaluation Evaluate(string toolName, string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return new ObscuraExecPolicyEvaluation(ExecPolicyDecision.Allow, null, null);

        var normalized = target.Trim();
        var useScriptRules = UsesScriptRules(toolName, normalized);
        var rules = useScriptRules ? _scriptRules : _urlRules;
        foreach (var rule in rules)
        {
            if (!ObscuraExecPolicyMatcher.Matches(normalized, rule.Pattern))
                continue;

            return new ObscuraExecPolicyEvaluation(
                rule.Decision,
                rule.Pattern,
                $"matched {rule.Kind} rule: {rule.Pattern}");
        }

        if (useScriptRules)
            return new ObscuraExecPolicyEvaluation(ExecPolicyDecision.Allow, null, "no script rule matched");

        return new ObscuraExecPolicyEvaluation(_defaultDecision, null, "default decision");
    }

    public void Audit(ObscuraExecPolicyAuditEntry entry)
    {
        lock (_lock)
            _audit.Add(entry);
    }

    public IReadOnlyList<ObscuraExecPolicyAuditEntry> GetAudit(Guid? runId) =>
        _audit.Where(a => a.RunId == runId).ToList();

    private static bool UsesScriptRules(string toolName, string target) =>
        string.Equals(toolName, "browser_execute_js", StringComparison.OrdinalIgnoreCase)
        || LooksLikeScript(target);

    private static bool LooksLikeScript(string target) =>
        target.Contains("document.", StringComparison.OrdinalIgnoreCase)
        || target.Contains("window.", StringComparison.OrdinalIgnoreCase)
        || target.Contains("localStorage", StringComparison.OrdinalIgnoreCase)
        || target.Contains("fetch(", StringComparison.OrdinalIgnoreCase)
        || target.StartsWith("(", StringComparison.Ordinal)
        || target.StartsWith("function", StringComparison.OrdinalIgnoreCase);

    private static (ExecPolicyDecision Default, IReadOnlyList<ObscuraExecPolicyRule> UrlRules, IReadOnlyList<ObscuraExecPolicyRule> ScriptRules)
        LoadRules(string path)
    {
        var resolved = ResolvePolicyPath(path);
        if (!File.Exists(resolved))
            return (ExecPolicyDecision.Prompt, DefaultUrlRules(), DefaultScriptRules());

        try
        {
            var yaml = File.ReadAllText(resolved);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            var doc = deserializer.Deserialize<ObscuraExecPolicyDocument>(yaml);
            if (doc is null)
                return (ExecPolicyDecision.Prompt, DefaultUrlRules(), DefaultScriptRules());

            var defaultDecision = Enum.TryParse<ExecPolicyDecision>(doc.Default, true, out var parsed)
                ? parsed
                : ExecPolicyDecision.Prompt;

            var urlRules = doc.UrlRules?.Select(r => MapRule(r, "url")).ToList() ?? DefaultUrlRules();
            var scriptRules = doc.ScriptRules?.Select(r => MapRule(r, "script")).ToList() ?? DefaultScriptRules();

            return (defaultDecision, urlRules, scriptRules);
        }
        catch
        {
            return (ExecPolicyDecision.Prompt, DefaultUrlRules(), DefaultScriptRules());
        }
    }

    private static ObscuraExecPolicyRule MapRule(ObscuraExecPolicyRuleYaml rule, string kind) =>
        new(
            kind,
            rule.Pattern ?? "*",
            Enum.TryParse<ExecPolicyDecision>(rule.Decision, true, out var d)
                ? d
                : ExecPolicyDecision.Prompt);

    private static IReadOnlyList<ObscuraExecPolicyRule> DefaultUrlRules() =>
    [
        new("url", "file://*", ExecPolicyDecision.Forbid),
        new("url", "http://localhost*", ExecPolicyDecision.Allow),
        new("url", "http://127.0.0.1*", ExecPolicyDecision.Allow),
        new("url", "https://localhost*", ExecPolicyDecision.Allow),
        new("url", "https://127.0.0.1*", ExecPolicyDecision.Allow),
        new("url", "https://*", ExecPolicyDecision.Prompt),
        new("url", "http://*", ExecPolicyDecision.Prompt),
    ];

    private static IReadOnlyList<ObscuraExecPolicyRule> DefaultScriptRules() =>
    [
        new("script", "*localStorage*", ExecPolicyDecision.Forbid),
        new("script", "*sessionStorage*", ExecPolicyDecision.Forbid),
        new("script", "*document.cookie*", ExecPolicyDecision.Forbid),
        new("script", "*navigator.sendBeacon*", ExecPolicyDecision.Forbid),
        new("script", "*fetch(*", ExecPolicyDecision.Prompt),
        new("script", "*XMLHttpRequest*", ExecPolicyDecision.Prompt),
    ];

    private static string ResolvePolicyPath(string configured)
    {
        if (Path.IsPathRooted(configured))
            return configured;

        var fromBase = Path.Combine(AppContext.BaseDirectory, configured);
        return File.Exists(fromBase) ? fromBase : configured;
    }

    private sealed class ObscuraExecPolicyDocument
    {
        public string Default { get; set; } = "prompt";
        public List<ObscuraExecPolicyRuleYaml>? UrlRules { get; set; }
        public List<ObscuraExecPolicyRuleYaml>? ScriptRules { get; set; }
    }

    private sealed class ObscuraExecPolicyRuleYaml
    {
        public string? Kind { get; set; }
        public string? Pattern { get; set; }
        public string? Decision { get; set; }
    }
}

internal static class ObscuraExecPolicyMatcher
{
    public static bool Matches(string value, string pattern)
    {
        if (pattern == "*")
            return true;

        if (pattern.EndsWith('*') && pattern.IndexOf('*') == pattern.Length - 1)
            return value.StartsWith(pattern.TrimEnd('*'), StringComparison.OrdinalIgnoreCase);

        return Regex.IsMatch(
            value,
            "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
