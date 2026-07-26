using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VeterinaryApi.Infrastructure.OutboxMessages;

/// <summary>EF Core fluent configuration for the <see cref="OutboxMessage"/> entity, mapping to the <c>outbox_messages</c> table.</summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <summary>Configures columns, primary key, and a partial index for unprocessed messages efficient polling.</summary>
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(o => o.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(o => o.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(o => o.CreatedOnUtc)
            .HasColumnName("created_on_utc")
            .IsRequired();

        builder.Property(o => o.ProcessedOnUtc)
            .HasColumnName("processed_on_utc");

        builder.Property(o => o.Errors)
            .HasColumnName("errors")
            .HasColumnType("text");

        // Index for unprocessed messages (for efficient polling)
        builder.HasIndex(o => o.ProcessedOnUtc)
            .HasDatabaseName("IX_outbox_messages_processed_on_utc")
            .HasFilter("processed_on_utc IS NULL");
    }
}
