using Microsoft.EntityFrameworkCore;
using Modules.Shared.Infrastructure.Outbox;

namespace Modules.Shared.Infrastructure.Persistence;

/// <summary>
/// DbContext for the <c>shared</c> schema. Owns the <c>shared.outbox_messages</c> table
/// migrations; every module DbContext also maps the outbox table with
/// <c>ExcludeFromMigrations</c> so the <c>InsertOutboxMessagesInterceptors</c> can write
/// domain events within each module's own transaction.
/// </summary>
public class SharedDbContext : DbContext
{
    public SharedDbContext(DbContextOptions<SharedDbContext> options) : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("shared");
        modelBuilder.ConfigureOutboxMessage();
    }
}
