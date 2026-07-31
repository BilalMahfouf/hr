using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PublicApi.Domain.Subscriptions;

namespace PublicApi.Infrastructure.Persistence.Configurations.Subscriptions;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.ToTable("subscription_plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasColumnName("slug")
            .HasMaxLength(120)
            .IsRequired();

        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            price.Property(m => m.Currency)
                .HasColumnName("price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(p => p.BillingInterval)
            .HasColumnName("billing_interval")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.IntervalCount)
            .HasColumnName("interval_count")
            .IsRequired();

        builder.Property(p => p.TrialDays)
            .HasColumnName("trial_days")
            .IsRequired();

        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(p => p.CreatedOnUtc)
            .HasColumnName("created_on_utc")
            .IsRequired();

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted");

        builder.Property(p => p.DeletedOnUtc)
            .HasColumnName("deleted_on_utc");

        builder.HasIndex(p => p.Slug)
            .IsUnique()
            .HasDatabaseName("ix_subscription_plans_slug");
    }
}
