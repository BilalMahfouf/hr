using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PublicApi.Domain.Notifications;

namespace PublicApi.Infrastructure.Persistence.Configurations.Notifications;

/// <summary>EF Core fluent configuration for the <see cref="Notification"/> entity, mapping to the <c>notifications</c> table.</summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    /// <summary>Configures soft-delete global query filter, table mapping, and column mappings.</summary>
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasQueryFilter(n => !n.IsDeleted);

        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(n => n.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.Body)
            .HasColumnName("body")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(n => n.IsRead)
            .HasColumnName("is_read")
            .IsRequired();

        builder.Property(n => n.CreatedOnUtc)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(n => n.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(n => n.DeletedOnUtc)
            .HasColumnName("deleted_at");

        builder.Property(n => n.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.HasIndex(n => n.TenantId)
            .HasDatabaseName("ix_notifications_tenant_id");
    }
}
