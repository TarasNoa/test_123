using Libr4.Shared.Kernel.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Shared.Infrastructure.Persistence;

/// <summary>
/// Base DbContext that dispatches domain events on SaveChangesAsync.
/// </summary>
public abstract class DbContextBase : DbContext
{
    private readonly IPublisher _publisher;

    protected DbContextBase(DbContextOptions options, IPublisher publisher) : base(options)
    {
        _publisher = publisher;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker.Entries()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        foreach (var a in aggregates) a.ClearDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var ev in events)
            await _publisher.Publish(ev, cancellationToken);

        return result;
    }
}

