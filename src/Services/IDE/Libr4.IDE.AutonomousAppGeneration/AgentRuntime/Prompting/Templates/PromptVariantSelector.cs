using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting.Templates;

public sealed class PromptVariantSelector
{
    private readonly PromptTemplateOptions _options;

    public PromptVariantSelector(IOptions<PromptTemplateOptions> options) =>
        _options = options.Value;

    public string SelectVariant(string role, Guid? runId = null)
    {
        if (!_options.AbVariants.TryGetValue(role, out var variants) || variants.Length == 0)
            return _options.DefaultVariant;

        if (variants.Length == 1)
            return variants[0];

        if (runId is not Guid id)
            return variants[0];

        var bucket = Math.Abs(id.GetHashCode()) % variants.Length;
        return variants[bucket];
    }
}
