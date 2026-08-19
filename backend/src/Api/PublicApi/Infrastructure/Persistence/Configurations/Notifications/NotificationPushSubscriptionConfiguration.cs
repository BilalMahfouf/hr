using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PublicApi.Domain.Notifications;

namespace PublicApi.Infrastructure.Persistence.Configurations.Notifications;

/// <summary>
/// EF Core fluent configuration for the <see cref="NotificationPushSubscription"/> entity,
/// mapping to the <c>notification_push_subscriptions</c> table.
/// </summary>
public class NotificationPushSubscriptionConfiguration
    : IEntityTypeConfiguration<NotificationPushSubscription>
{
    /// <summary>Configures table name, primary key, column mappings, and indexes.</summary>
    public void Configure(EntityTypeBuilder<NotificationPushSubscription> builder)
    {
        builder.ToTable("notification_push_subscriptions");

        builder.HasKey(e => e.Id);

        // Soft-delete filter — consistent with all other entity configurations.
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.Endpoint)
            .HasColumnName("endpoint")
            .HasMaxLength(2048)
            .IsRequired();

        // Unique per endpoint so we can upsert instead of creating duplicates.
        builder.HasIndex(e => e.Endpoint)
            .HasDatabaseName("ix_notification_push_subscriptions_endpoint")
            .IsUnique();

        builder.Property(e => e.P256dh)
            .HasColumnName("p256dh")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(e => e.Auth)
            .HasColumnName("auth")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(512);

        builder.Property(e => e.CreatedOnUtc)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(e => e.DeletedOnUtc)
            .HasColumnName("deleted_at");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_notification_push_subscriptions_user_id");
    }
}
