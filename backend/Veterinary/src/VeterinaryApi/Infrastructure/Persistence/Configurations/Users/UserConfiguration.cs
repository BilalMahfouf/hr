using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Infrastructure.Persistence.Configurations.Users;

/// <summary>EF Core fluent configuration for the <see cref="User"/> entity, mapping to the <c>users</c> table.</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>Configures soft-delete global query filter, table mapping, keys, owned types (Email, Name), and relationships.</summary>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(u => u.UserName)
            .HasColumnName("user_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedOnUtc)
            .HasColumnName("created_on_utc");

        builder.Property(u => u.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        // Index on TenantId
        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("ix_users_tenant_id");

        // Unique index on Email within Tenant
        builder.HasIndex(u => new { u.TenantId, u.Email })
            .IsUnique()
            .HasDatabaseName("ix_users_tenant_id_email");

        // Unique index on UserName within Tenant
        builder.HasIndex(u => new { u.TenantId, u.UserName })
            .IsUnique()
            .HasDatabaseName("ix_users_tenant_id_user_name");

        // Configure backing field for Sessions
        builder.Navigation(u => u.Sessions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

    }

}
