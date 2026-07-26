using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Common;
using Identity.Domain.Users;

namespace VeterinaryApi.Infrastructure.OutboxMessages
{
    /// <summary>
    /// An EF Core <see cref="SaveChangesInterceptor"/> that captures all domain events
    /// raised by tracked <see cref="Entity"/> instances before the database transaction is committed,
    /// and persists them as <see cref="OutboxMessage"/> rows in the same transaction.
    /// </summary>
    /// <remarks>
    /// This is the <em>write</em> half of the Outbox Pattern. By intercepting
    /// <c>SavingChangesAsync</c>, domain events are stored atomically alongside the
    /// aggregate state changes that produced them. This guarantees that no event is lost
    /// even if the application crashes immediately after saving — the event will be
    /// discovered and re-published by <see cref="ProcessOutboxMessagesJob"/> on restart.
    ///
    /// <b>Multi-tenancy:</b> The interceptor stamps each event's <c>TenantId</c> from the
    /// current ambient tenant context (<see cref="ICurrentTenant"/>) so downstream handlers
    /// can filter or route by tenant.
    ///
    /// <b>Serialization:</b> Newtonsoft.Json with <c>TypeNameHandling.All</c> is used so that
    /// the concrete event type can be fully reconstructed during deserialization.
    /// </remarks>
    public class InsertOutboxMessagesInterceptors(
        ICurrentTenant currentTenant)
        : SaveChangesInterceptor
    {
        private static readonly JsonSerializerSettings _serializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        /// <summary>
        /// Called by EF Core immediately before executing the SQL <c>SaveChanges</c>.
        /// Delegates to <see cref="InsertOutboxMessage"/> to drain all pending domain events
        /// from tracked entities and insert them as outbox records within the same transaction.
        /// </summary>
        /// <param name="eventData">EF Core event data exposing the current <see cref="DbContext"/>.</param>
        /// <param name="result">The current interception result (passed through unchanged).</param>
        /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
        /// <returns>
        /// The base interception result forwarded from <see cref="SaveChangesInterceptor"/>.
        /// </returns>
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

        /// <summary>
        /// Collects all domain events from every tracked <see cref="Entity"/>, clears the entity's
        /// internal event list, stamps the current tenant identifier onto each event, then
        /// serializes them into <see cref="OutboxMessage"/> rows and adds them to the context.
        /// </summary>
        /// <remarks>
        /// Entity event lists are cleared immediately after collection so that the same events
        /// are not duplicated if <c>SaveChanges</c> is called multiple times on the same context.
        /// </remarks>
        /// <param name="context">The active EF Core <see cref="DbContext"/> whose change tracker is inspected.</param>
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

            foreach (var @event in events)
            {
                if(@event is UserForgetPasswordDomainEvent)
                {
                    continue;
                }
                @event.TenantId = currentTenant.UserId!.Value;
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
}
