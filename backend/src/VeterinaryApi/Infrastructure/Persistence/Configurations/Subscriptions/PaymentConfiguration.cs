using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VeterinaryApi.Domain.Subscriptions;

namespace VeterinaryApi.Infrastructure.Persistence.Configurations.Subscriptions;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.ToTable("subscription_payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.SubscriptionId)
            .HasColumnName("subscription_id")
            .IsRequired();

        builder.HasOne(p => p.Subscription)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_subscription_payments_subscriptions_subscription_id");

        builder.Property(p => p.DoctorId)
            .HasColumnName("doctor_id")
            .IsRequired();

        builder.OwnsOne(p => p.Amount, amount =>
        {
            amount.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();

            amount.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<byte>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(p => p.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.ProviderPaymentId)
            .HasColumnName("provider_payment_id")
            .HasMaxLength(120);

        builder.Property(p => p.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.ProviderMetadata)
            .HasColumnName("provider_metadata")
            .HasColumnType("text");

        builder.Property(p => p.FailureReason)
            .HasColumnName("failure_reason")
            .HasColumnType("text");

        builder.Property(p => p.PaidAt)
            .HasColumnName("paid_at");

        builder.Property(p => p.CreatedOnUtc)
            .HasColumnName("created_on_utc")
            .IsRequired();

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted");

        builder.Property(p => p.DeletedOnUtc)
            .HasColumnName("deleted_on_utc");

        builder.Property(p => p.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("ix_subscription_payments_tenant_id");

        builder.HasIndex(p => p.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("ix_subscription_payments_idempotency_key");
    }
}
