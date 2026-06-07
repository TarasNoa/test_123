using Libr4.AI.Application.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;
using Libr4.IDE.Application.AutonomousAppGeneration.HostProfiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.BatchCi;

public interface IAutonomousBatchLlmProfileScope
{
    IDisposable? BeginRunScope(bool useBatchProfile);

    bool ShouldUseBatchProfile(string? triggerSource);
}

public sealed class AutonomousBatchLlmProfileScope : IAutonomousBatchLlmProfileScope
{
    private readonly AutonomousBatchLlmProfileOptions _options;
    private readonly AutonomousHostProfileOptions _hostProfile;
    private readonly ILogger<AutonomousBatchLlmProfileScope> _logger;

    public AutonomousBatchLlmProfileScope(
        IOptions<AutonomousBatchLlmProfileOptions> options,
        IOptions<AutonomousHostProfileOptions> hostProfile,
        ILogger<AutonomousBatchLlmProfileScope> logger)
    {
        _options = options.Value;
        _hostProfile = hostProfile.Value;
        _logger = logger;
    }

    public bool ShouldUseBatchProfile(string? triggerSource) =>
        _hostProfile.ActiveProfile == AutonomousHostProfile.BatchCi
        || _options.UseBatchLlmProfile
        || string.Equals(triggerSource, "ci", StringComparison.OrdinalIgnoreCase)
        || string.Equals(triggerSource, "nightly", StringComparison.OrdinalIgnoreCase)
        || string.Equals(triggerSource, "nightly-ci", StringComparison.OrdinalIgnoreCase);

    public IDisposable? BeginRunScope(bool useBatchProfile)
    {
        if (!useBatchProfile)
            return null;

        _logger.LogInformation(
            "Activating batch LLM profile: model={Model}, disableStreaming={DisableStreaming}",
            _options.Model,
            _options.DisableStreaming);

        return LlmCallPreferenceContext.Activate(new LlmCallPreferences(
            ModelOverride: _options.Model,
            DisableStreaming: _options.DisableStreaming));
    }
}
