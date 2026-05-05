using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Hooks.BuiltIn;

public class ContextCompressionHook : IHook
{
    private readonly ILogger<ContextCompressionHook> _logger;
    private const int MaxContextLength = 100000; // 100k characters

    public HookType Type => HookType.PreCompact;
    public string Name => "ContextCompression";

    public ContextCompressionHook(ILogger<ContextCompressionHook> logger)
    {
        _logger = logger;
    }

    public Task<HookResult> ExecuteAsync(HookContext context)
    {
        if (context.Result is string resultString && resultString.Length > MaxContextLength)
        {
            var compressed = resultString.Substring(0, MaxContextLength) + "... [truncated]";
            _logger.LogInformation(
                "Context compressed from {OriginalLength} to {CompressedLength} characters",
                resultString.Length,
                compressed.Length
            );

            return Task.FromResult(new HookResult
            {
                ShouldContinue = true,
                ModifiedResult = compressed
            });
        }

        return Task.FromResult(new HookResult { ShouldContinue = true });
    }
}
