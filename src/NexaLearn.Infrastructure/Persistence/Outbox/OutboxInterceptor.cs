using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexaLearn.Domain.Common;
using System.Text.Json;

namespace NexaLearn.Infrastructure.Persistence.Outbox;

internal sealed class OutboxInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var outboxMessages = eventData.Context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .SelectMany(entry =>
            {
                var events = entry.Entity.DomainEvents;
                entry.Entity.ClearDomainEvents();
                return events;
            })
            .Select(domainEvent => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = domainEvent.GetType().FullName!,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
                OccurredAt = DateTimeOffset.UtcNow
            })
            .ToList();

        if (outboxMessages.Count > 0)
            eventData.Context.Set<OutboxMessage>().AddRange(outboxMessages);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
