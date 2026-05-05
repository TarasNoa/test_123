using Libr4.Shared.Contracts.IntegrationEvents.Auth;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Libr4.Tasks.Infrastructure.Messaging;

/// <summary>
/// Caches user information locally when a user registers.
/// This allows the Tasks service to display user names without querying the Auth service.
/// </summary>
public sealed class UserRegisteredConsumer : IConsumer<UserRegisteredIntegrationEvent>
{
    private readonly IDistributedCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public UserRegisteredConsumer(IDistributedCache cache) => _cache = cache;

    public async Task Consume(ConsumeContext<UserRegisteredIntegrationEvent> context)
    {
        var e = context.Message;

        var userInfo = new UserInfo(e.UserId, e.Email, e.DisplayName);

        await _cache.SetStringAsync(
            $"user:{e.UserId}",
            JsonSerializer.Serialize(userInfo),
            new DistributedCacheEntryOptions { SlidingExpiration = CacheDuration },
            context.CancellationToken);
    }

    private sealed record UserInfo(Guid Id, string Email, string DisplayName);
}
