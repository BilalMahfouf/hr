using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using Modules.Shared.Domain.Common;

namespace Modules.Shared.Infrastructure.Outbox;

public class InsertOutboxMessagesInterceptors : SaveChangesInterceptor
{
    private static readonly JsonSerializerSettings _serializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            InsertOutboxMessage(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void InsertOutboxMessage(DbContext context)
    {
        var events = context.ChangeTracker
                       .Entries<Entity>()
                       .Select(entry => entry.Entity)
                       .SelectMany(e =>
                       {
                           var domainEvents = e.DomainEvents.ToList();
                           e.ClearDomainEvent();
                           return domainEvents;
                       }).ToList();

        if (events.Count == 0)
        {
            return;
        }

        var outboxMessages = events
            .Select(@event => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Name = @event.GetType().AssemblyQualifiedName!,
                Content = JsonConvert.SerializeObject(@event, _serializerSettings),
                CreatedOnUtc = DateTime.UtcNow
            }).ToList();

        context.Set<OutboxMessage>().AddRange(outboxMessages);
    }
}
