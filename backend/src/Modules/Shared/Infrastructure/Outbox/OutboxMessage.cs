namespace Modules.Shared.Infrastructure.Outbox;

public class OutboxMessage
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ProcessedOnUtc { get; set; }

    public int RetryCount { get; set; }

    public string? LastError { get; set; }

    public DateTime? LastAttemptOnUtc { get; set; }
}
