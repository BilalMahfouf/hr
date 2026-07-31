using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quartz;
using Modules.Shared.CQRS;
using Modules.Shared.Domain.Common;
using Modules.Shared.Infrastructure.Outbox;
using PublicApi.Infrastructure.Persistence;

namespace PublicApi.Infrastructure.OutboxMessages;

[DisallowConcurrentExecution]
public class ProcessOutboxMessagesJob : IJob
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IDomainEventPublisher _publisher;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    private static readonly JsonSerializerSettings _serializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };

    private const int MaxRetries = 10;

    public ProcessOutboxMessagesJob(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IDomainEventPublisher publisher,
        ILogger<ProcessOutboxMessagesJob> logger)
    {
        _contextFactory = contextFactory;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(context.CancellationToken);

        var outboxMessages = await dbContext.Set<OutboxMessage>()
            .OrderBy(e => e.Id)
            .Where(e => e.ProcessedOnUtc == null)
            .Take(20)
            .ToListAsync(context.CancellationToken);

        if (outboxMessages.Count == 0)
            return;

        foreach (var outboxMessage in outboxMessages)
        {
            try
            {
                var domainEvent = DeserializeDomainEvent(outboxMessage);
                if (domainEvent is null)
                    continue;

                await _publisher.PublishAsync(domainEvent, context.CancellationToken);

                outboxMessage.ProcessedOnUtc = DateTime.UtcNow;
                _logger.LogInformation("Event {EventType} processed", outboxMessage.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", outboxMessage.Id);

                outboxMessage.RetryCount++;
                outboxMessage.LastError = ex.Message;
                outboxMessage.LastAttemptOnUtc = DateTime.UtcNow;

                if (outboxMessage.RetryCount >= MaxRetries)
                {
                    outboxMessage.ProcessedOnUtc = DateTime.UtcNow;
                    _logger.LogWarning("Outbox message {MessageId} moved to dead-letter after {Retries} retries",
                        outboxMessage.Id, outboxMessage.RetryCount);
                }
            }
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
    }

    private static IDomainEvent? DeserializeDomainEvent(OutboxMessage outboxMessage)
    {
        var domainEventType = Type.GetType(outboxMessage.Name);
        if (domainEventType is null)
            return null;

        return JsonConvert
            .DeserializeObject(outboxMessage.Content, domainEventType, _serializerSettings) as IDomainEvent;
    }
}
