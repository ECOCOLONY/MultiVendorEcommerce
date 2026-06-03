using Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence.Interceptors;

public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _mediator;

    public DispatchDomainEventsInterceptor(IPublisher mediator) => _mediator = mediator;

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        await DispatchEvents(eventData.Context, ct);
        return await base.SavedChangesAsync(eventData, result, ct);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchEvents(eventData.Context).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    private async Task DispatchEvents(DbContext? context, CancellationToken ct = default)
    {
        if (context is null) return;

        var entitiesWithEvents = context.ChangeTracker
            .Entries<BaseAuditableEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToArray();

        var events = entitiesWithEvents.SelectMany(e => e.DomainEvents).ToArray();
        foreach (var entity in entitiesWithEvents) entity.ClearDomainEvents();
        foreach (var ev in events) await _mediator.Publish(ev, ct);
    }
}
