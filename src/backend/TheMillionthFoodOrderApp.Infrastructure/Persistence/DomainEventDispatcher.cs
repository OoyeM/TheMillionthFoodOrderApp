using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Common;
using Wolverine;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence;

internal static class DomainEventDispatcher
{
    /// Collects all domain events from EF Core-tracked entities and clears them from the aggregates.
    /// Call AFTER the last mutation and BEFORE SaveChangesAsync so entities are still in the change tracker.
    internal static List<IDomainEvent> CollectAndClear(DbContext context)
    {
        var events = new List<IDomainEvent>();

        foreach (var entry in context.ChangeTracker.Entries<IHasDomainEvents>())
        {
            events.AddRange(entry.Entity.DomainEvents);
            entry.Entity.ClearDomainEvents();
        }

        return events;
    }

    /// Publishes each collected event via Wolverine. Call AFTER SaveChangesAsync (and after CommitAsync
    /// for transactional methods) so events are only dispatched once the database change is durable.
    internal static async Task PublishAsync(IEnumerable<IDomainEvent> events, IMessageBus bus)
    {
        foreach (var @event in events)
            await bus.PublishAsync((dynamic)@event);
    }
}
