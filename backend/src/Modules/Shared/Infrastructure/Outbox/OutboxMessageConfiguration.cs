using Microsoft.EntityFrameworkCore;

namespace Modules.Shared.Infrastructure.Outbox;

public static class OutboxMessageConfiguration
{
    public static void ConfigureOutboxMessage(this ModelBuilder builder, bool excludeFromMigrations = false)
    {
        builder.Entity<OutboxMessage>(cfg =>
        {
            cfg.ToTable("outbox_messages", "shared", t =>
            {
                if (excludeFromMigrations) t.ExcludeFromMigrations();
            });

            cfg.HasKey(o => o.Id);

            cfg.Property(o => o.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            cfg.Property(o => o.Name)
                .HasColumnName("name")
                .HasMaxLength(256)
                .IsRequired();

            cfg.Property(o => o.Content)
                .HasColumnName("content")
                .HasColumnType("jsonb")
                .IsRequired();

            cfg.Property(o => o.CreatedOnUtc)
                .HasColumnName("created_on_utc")
                .IsRequired();

            cfg.Property(o => o.ProcessedOnUtc)
                .HasColumnName("processed_on_utc");

            cfg.Property(o => o.RetryCount)
                .HasColumnName("retry_count");

            cfg.Property(o => o.LastError)
                .HasColumnName("last_error")
                .HasColumnType("text");

            cfg.Property(o => o.LastAttemptOnUtc)
                .HasColumnName("last_attempt_on_utc");

            cfg.HasIndex(o => o.ProcessedOnUtc)
                .HasDatabaseName("IX_outbox_messages_processed_on_utc")
                .HasFilter("\"processed_on_utc\" IS NULL");
        });
    }
}
