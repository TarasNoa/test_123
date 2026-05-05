using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Runtime;

public sealed class DefaultRuntimeCommandPolicy : IRuntimeCommandPolicy
{
    private readonly RuntimePolicyOptions _options;

    public DefaultRuntimeCommandPolicy(IOptions<RuntimePolicyOptions> options)
    {
        _options = options.Value;
    }

    public TimeSpan GetCommandTimeout(string command)
    {
        var seconds = Math.Clamp(_options.MaxCommandTimeoutSeconds, 10, 3600);
        return TimeSpan.FromSeconds(seconds);
    }

    public void EnsureCommandAllowed(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        var normalized = command.Trim().ToLowerInvariant();
        foreach (var pattern in _options.DenyCommandContains)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;
            if (normalized.Contains(pattern.Trim().ToLowerInvariant(), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Command blocked by runtime policy. Matched deny rule: '{pattern}'.");
            }
        }
    }
}
