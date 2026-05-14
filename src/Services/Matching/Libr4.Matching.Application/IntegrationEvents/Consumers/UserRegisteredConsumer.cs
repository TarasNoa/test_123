using Libr4.Matching.Application.Abstractions;
using Libr4.Matching.Domain.Profiles;
using Libr4.Shared.Contracts.IntegrationEvents.Auth;
using MassTransit;

namespace Libr4.Matching.Application.IntegrationEvents.Consumers;

public sealed class UserRegisteredConsumer : IConsumer<UserRegisteredIntegrationEvent>
{
    private readonly IMatchingService _matchingService;

    public UserRegisteredConsumer(IMatchingService matchingService)
    {
        _matchingService = matchingService;
    }

    public async Task Consume(ConsumeContext<UserRegisteredIntegrationEvent> context)
    {
        var msg = context.Message;

        // Index a minimal freelancer profile so the user is discoverable immediately.
        // Real skills/bio will be populated later via profile updates.
        var profile = new FreelancerMatchProfile
        {
            FreelancerId = msg.UserId,
            Skills = new List<string>(),
            Interests = new List<string>(),
            AverageRating = 0,
            CompletedTasks = 0,
            HourlyRateMin = 0,
            HourlyRateMax = 0,
            Embedding = new float[384],
            IndexedAt = DateTimeOffset.UtcNow
        };

        await _matchingService.IndexFreelancerAsync(profile, context.CancellationToken);
    }
}
