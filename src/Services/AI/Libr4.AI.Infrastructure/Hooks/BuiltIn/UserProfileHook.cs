using Libr4.AI.Infrastructure.Profile;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Hooks.BuiltIn;

/// <summary>
/// User Profile Hook - extracts and injects structured user profile
/// Based on NGT Memory pattern for structured user profiles
/// </summary>
public class UserProfileHook : IHook
{
    private readonly IUserProfileExtractor _profileExtractor;
    private readonly ILogger<UserProfileHook> _logger;
    private readonly string? _currentUserId;

    public UserProfileHook(
        IUserProfileExtractor profileExtractor,
        ILogger<UserProfileHook> logger,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _profileExtractor = profileExtractor;
        _logger = logger;
        
        _currentUserId = httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value
                      ?? httpContextAccessor?.HttpContext?.User?.FindFirst("user_id")?.Value
                      ?? "default_user";
    }

    public HookType Type => HookType.PreToolUse;
    public string Name => "UserProfile";
    public int Priority => 30; // Medium priority

    public async Task<HookResult> ExecuteAsync(HookContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(_currentUserId))
                return new HookResult { ShouldContinue = true };

            // Get formatted profile
            var profile = await _profileExtractor.GetFormattedProfileAsync(_currentUserId);
            
            if (!string.IsNullOrEmpty(profile))
            {
                context.Metadata["user_profile"] = profile;
                _logger.LogDebug("UserProfile: Injected profile for user {UserId}", _currentUserId);
            }

            return new HookResult { ShouldContinue = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserProfile: Failed to get profile");
            return new HookResult { ShouldContinue = true };
        }
    }
}
