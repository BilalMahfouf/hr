using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Identity.Domain.Users;

namespace Modules.Identity.Infrastructure.Persistence.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.Token)
            .HasColumnName("token")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(s => s.TokenType)
            .HasColumnName("token_type")
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(s => s.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(s => s.CreatedOnUtc)
            .HasColumnName("created_on_utc");

        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("ix_user_sessions_user_id");

        builder.HasIndex(s => s.Token)
            .HasDatabaseName("ix_user_sessions_token");

        builder.HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.HasIndex(s => s.TenantId)
            .HasDatabaseName("ix_user_sessions_tenant_id");
    }
}
