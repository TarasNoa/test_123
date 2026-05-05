using System.Collections.Concurrent;
using Scriban;
using Scriban.Runtime;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Templates;

/// <summary>
/// P1-8 of audit roadmap. Engine for parameterised fallback artefacts (README,
/// docker-compose, CI workflow, etc.) replacing the ~1.2k lines of hard-coded
/// strings in <c>LlmCodeGenerationService</c>.
///
/// Templates are Scriban (Liquid-like). Variables exposed:
///   * <c>app_name</c> — sanitised application name
///   * <c>port</c> — runtime port (default 4000)
///   * <c>stack</c> — "dotnet" | "python" | "node"
///   * <c>language</c> / <c>framework</c> / <c>database</c>
///   * <c>extras</c> — caller-supplied dictionary
///
/// Templates are cached after first parse (LRU not necessary — bounded set).
/// </summary>
public interface IFallbackArtefactTemplateEngine
{
    string Render(string templateText, FallbackTemplateContext context);
}

public sealed class FallbackTemplateContext
{
    public string AppName { get; init; } = "GeneratedApp";
    public string Stack { get; init; } = "dotnet";
    public string Language { get; init; } = "csharp";
    public string Framework { get; init; } = "asp.net";
    public string Database { get; init; } = "postgres";
    public int Port { get; init; } = 4000;
    public IReadOnlyDictionary<string, string> Extras { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);
}

public sealed class ScribanFallbackTemplateEngine : IFallbackArtefactTemplateEngine
{
    private readonly ConcurrentDictionary<string, Template> _cache = new(StringComparer.Ordinal);

    public string Render(string templateText, FallbackTemplateContext context)
    {
        if (string.IsNullOrEmpty(templateText)) return string.Empty;

        var template = _cache.GetOrAdd(templateText, t =>
        {
            var parsed = Template.Parse(t);
            // If the template has parse errors, fall back to a literal-output template
            // so callers never see exceptions. The error is surfaced via TemplateException
            // when called explicitly, but Render guarantees a string.
            return parsed;
        });

        if (template.HasErrors)
        {
            // Surface a deterministic, debuggable artefact instead of throwing.
            return $"# template_parse_error\n# {string.Join("\n# ", template.Messages)}\n";
        }

        var so = new ScriptObject();
        so.Add("app_name", SanitizeName(context.AppName));
        so.Add("stack", context.Stack);
        so.Add("language", context.Language);
        so.Add("framework", context.Framework);
        so.Add("database", context.Database);
        so.Add("port", context.Port);
        so.Add("extras", context.Extras);

        var ctx = new TemplateContext { StrictVariables = false };
        ctx.PushGlobal(so);
        try
        {
            return template.Render(ctx);
        }
        catch (Exception ex)
        {
            return $"# template_render_error\n# {ex.GetType().Name}: {ex.Message}\n";
        }
    }

    private static string SanitizeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "GeneratedApp";
        return raw.Replace('"', ' ').Replace('\n', ' ').Trim();
    }
}
