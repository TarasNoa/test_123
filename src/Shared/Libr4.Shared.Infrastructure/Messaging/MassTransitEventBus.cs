using Libr4.Shared.Kernel.Application;
using MassTransit;

namespace Libr4.Shared.Infrastructure.Messaging;

public sealed class MassTransitEventBus : IEventBus
{
    private readonly IPublishEndpoint _publish;

    public MassTransitEventBus(IPublishEndpoint publish) => _publish = publish;

    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : class
        => _publish.Publish(integrationEvent, cancellationToken);
}
