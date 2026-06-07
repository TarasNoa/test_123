using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IUserProfileStore _store;
    private readonly UserProfileOptions _options;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(
        IUserProfileStore store,
        IOptions<UserProfileOptions> options,
        ILogger<UserProfileService> logger)
    {
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    public string? ResolveUserId(AppGenerationOrchestrator orchestrator) =>
        _options.Enabled ? UserProfileIdentityResolver.Resolve(orchestrator) : null;

    public async Task<string> AugmentPlanningRequestAsync(
        AppGenerationOrchestrator orchestrator,
        string userRequest,
        CancellationToken ct = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(userRequest))
            return userRequest;

        var userId = ResolveUserId(orchestrator);
        if (userId is null)
            return userRequest;

        var profile = await _store.LoadAsync(userId, ct).ConfigureAwait(false);
        if (profile is null
            || (profile.PreferredStacks.Count == 0
                && profile.RecurringFailures.Count == 0
                && profile.SuccessfulPatterns.Count == 0))
        {
            return userRequest;
        }

        var section = profile.ToPlanningSection(_options.MaxPlanningChars);
        return $"{section}\n\n{userRequest}";
    }

    public async Task UpdateFromRunAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        var userId = ResolveUserId(orchestrator);
        if (userId is null)
            return;

        if (orchestrator.Status != GenerationStatus.Completed
            && orchestrator.Status != GenerationStatus.Failed)
        {
            return;
        }

        var profile = await _store.LoadAsync(userId, ct).ConfigureAwait(false)
            ?? new UserProfileDocument { UserId = userId };

        var now = DateTime.UtcNow;
        if (orchestrator.Plan is not null)
            UpsertStack(profile, BuildStackSignature(orchestrator.Plan.TechStack), now);

        foreach (var failure in CollectFailures(orchestrator))
            UpsertFailure(profile, failure, now);

        if (orchestrator.Status == GenerationStatus.Completed)
        {
            var pattern = BuildSuccessPattern(orchestrator);
            if (!string.IsNullOrWhiteSpace(pattern))
                UpsertSuccess(profile, pattern, orchestrator.Iterations.Count, now);
        }

        profile.UpdatedAtUtc = now;
        Trim(profile);
        await _store.SaveAsync(profile, ct).ConfigureAwait(false);
        _logger.LogInformation("Updated USER.profile.md for user {UserId}", userId);
    }

    private void UpsertStack(UserProfileDocument profile, string stack, DateTime now)
    {
        var existing = profile.PreferredStacks.FirstOrDefault(entry =>
            string.Equals(entry.Stack, stack, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            profile.PreferredStacks.Add(new ProfileStackEntry(stack, 1, now));
            return;
        }

        profile.PreferredStacks.Remove(existing);
        profile.PreferredStacks.Add(existing with { RunCount = existing.RunCount + 1, LastSeenUtc = now });
    }

    private void UpsertFailure(UserProfileDocument profile, string signature, DateTime now)
    {
        var existing = profile.RecurringFailures.FirstOrDefault(entry =>
            string.Equals(entry.Signature, signature, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            profile.RecurringFailures.Add(new ProfileFailureEntry(signature, 1, now));
            return;
        }

        profile.RecurringFailures.Remove(existing);
        profile.RecurringFailures.Add(existing with
        {
            OccurrenceCount = existing.OccurrenceCount + 1,
            LastSeenUtc = now
        });
    }

    private void UpsertSuccess(UserProfileDocument profile, string pattern, int iterations, DateTime now)
    {
        profile.SuccessfulPatterns.RemoveAll(entry =>
            string.Equals(entry.Pattern, pattern, StringComparison.OrdinalIgnoreCase));
        profile.SuccessfulPatterns.Add(new ProfileSuccessEntry(pattern, iterations, now));
    }

    private void Trim(UserProfileDocument profile)
    {
        while (profile.PreferredStacks.Count > _options.MaxPreferredStacks)
        {
            var remove = profile.PreferredStacks
                .OrderBy(entry => entry.RunCount)
                .ThenBy(entry => entry.LastSeenUtc)
                .First();
            profile.PreferredStacks.Remove(remove);
        }

        while (profile.RecurringFailures.Count > _options.MaxRecurringFailures)
        {
            var remove = profile.RecurringFailures
                .OrderBy(entry => entry.OccurrenceCount)
                .ThenBy(entry => entry.LastSeenUtc)
                .First();
            profile.RecurringFailures.Remove(remove);
        }

        while (profile.SuccessfulPatterns.Count > _options.MaxSuccessfulPatterns)
        {
            var remove = profile.SuccessfulPatterns
                .OrderBy(entry => entry.CompletedAtUtc)
                .First();
            profile.SuccessfulPatterns.Remove(remove);
        }
    }

    private static string BuildStackSignature(TechStack stack)
    {
        var parts = stack.Languages
            .Concat(stack.Frameworks)
            .Concat(stack.Databases)
            .Select(part => part.Trim().ToLowerInvariant())
            .Where(part => part.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return string.Join("|", parts);
    }

    private static IEnumerable<string> CollectFailures(AppGenerationOrchestrator orchestrator)
    {
        if (!string.IsNullOrWhiteSpace(orchestrator.FailureReason))
            yield return orchestrator.FailureReason.Trim();

        foreach (var iteration in orchestrator.Iterations)
        {
            foreach (var error in iteration.Errors)
            {
                var signature = $"{error.ErrorType}: {Truncate(error.Message, 120)}";
                if (!string.IsNullOrWhiteSpace(error.FilePath))
                    signature += $" @ {error.FilePath}";
                yield return signature.Trim();
            }
        }
    }

    private static string BuildSuccessPattern(AppGenerationOrchestrator orchestrator)
    {
        var plan = orchestrator.Plan;
        if (plan is null)
            return string.Empty;

        var stack = plan.TechStack.Frameworks.FirstOrDefault()
            ?? plan.TechStack.Languages.FirstOrDefault()
            ?? "unknown-stack";
        var requestHint = Truncate(orchestrator.UserRequest.Replace('\n', ' ').Trim(), 80);
        return $"{requestHint} + {stack}";
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
