using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quartz;
using Modules.Shared.Abstracions;
using Modules.Shared.CQRS;
using Modules.Shared.Domain.Common;
using PublicApi.Infrastructure.Persistence;

namespace PublicApi.Infrastructure.OutboxMessages;

/// <summary>
/// A Quartz.NET background job that processes pending messages from the outbox table.
/// This job is the delivery component of the Outbox Pattern:
/// it reads serialized domain events from the <c>OutboxMessages</c> table and
/// dispatches them to their registered <see cref="IDomainEventHandler{T}"/> implementations.
/// </summary>
/// <remarks>
/// The job runs every 10 seconds and processes up to 20 messages per run.
/// It is decorated with <see cref="DisallowConcurrentExecutionAttribute"/> to prevent
/// multiple concurrent executions that could lead to double-processing of events.
///
/// If a message's domain event type cannot be resolved or deserialized, the message
/// is silently skipped. The <c>ProcessedOnUtc</c> timestamp is only set after
/// successful handler invocation, providing at-least-once delivery semantics.
///
/// <b>Known limitation:</b> There is currently no error handling; a handler failure
/// will cause the entire batch's <c>SaveChangesAsync</c> to not mark that message as
/// processed, causing an infinite retry. Consider wrapping in try-catch with error recording.
/// </remarks>
[DisallowConcurrentExecution]
public class ProcessOutboxMessagesJob : IJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IDomainEventPublisher _publisher;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    /// <summary>
    /// Initializes the job with required dependencies.
    /// </summary>
    /// <param name="context">The EF Core database context for reading outbox messages.</param>
    /// <param name="domainEventDispatcher">The sequential dispatcher (currently unused in this implementation).</param>
    /// <param name="publisher">The parallel event publisher that invokes all handlers for an event.</param>
    public ProcessOutboxMessagesJob(
        ApplicationDbContext context,
        IDomainEventDispatcher domainEventDispatcher,
        IDomainEventPublisher publisher,
        ILogger<ProcessOutboxMessagesJob> logger)
    {
        _dbContext = context;
        _domainEventDispatcher = domainEventDispatcher;
        _publisher = publisher;
        _logger = logger;
    }

    private static readonly JsonSerializerSettings _serializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };

    /// <summary>
    /// Entry point called by the Quartz scheduler every 10 seconds.
    /// Reads the next batch of pending outbox messages, deserializes each domain event,
    /// publishes it to all handlers, and marks it as processed.
    /// </summary>
    /// <param name="context">The Quartz job execution context providing a cancellation token.</param>
    public async Task Execute(IJobExecutionContext context)
    {
        var outboxMessages = await _dbContext.Set<OutboxMessage>()
            .OrderBy(e => e.Id)
            .Where(e => e.ProcessedOnUtc == null)
            .Take(20)
            .ToListAsync(context.CancellationToken);
        if (outboxMessages is null || !outboxMessages.Any())
        {
            return;
        }
        List<IDomainEvent> events = new();
        foreach (var outboxMessage in outboxMessages)
        {
            var domainEvent = DeserializeDomainEvent(outboxMessage);
            if (domainEvent is null)
            {
                continue;
            }
            await _publisher.PublishAsync(domainEvent, context.CancellationToken);
            _logger.LogInformation($"event {domainEvent.ToString()} is proccessed");
            outboxMessage.ProcessedOnUtc = DateTime.UtcNow;

        }
        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }

    /// <summary>
    /// Deserializes an <see cref="OutboxMessage"/> back into its original domain event type.
    /// Uses Newtonsoft.Json with <c>TypeNameHandling.All</c> to reconstruct the correct concrete type.
    /// </summary>
    /// <param name="outboxMessage">The outbox message containing the serialized domain event.</param>
    /// <returns>
    /// The deserialized <see cref="IDomainEvent"/> instance, or <c>null</c> if deserialization fails.
    /// </returns>
    private static IDomainEvent? DeserializeDomainEvent(OutboxMessage outboxMessage)
    {
        var domainEventType = Type.GetType(outboxMessage.Name)!;
        var domainEvent = JsonConvert
            .DeserializeObject(
            outboxMessage.Content, domainEventType, _serializerSettings) as IDomainEvent;
        return domainEvent;
    }
}
