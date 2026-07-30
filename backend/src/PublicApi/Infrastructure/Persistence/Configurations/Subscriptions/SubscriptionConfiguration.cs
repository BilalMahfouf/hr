using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PublicApi.Domain.Subscriptions;

namespace PublicApi.Infrastructure.Persistence.Configurations.Subscriptions;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(s => s.DoctorId)
            .HasColumnName("doctor_id")
            .IsRequired();

        builder.Property(s => s.PreviousSubscriptionId)
            .HasColumnName("previous_subscription_id");

        builder.HasOne(s => s.PreviousSubscription)
            .WithMany()
            .HasForeignKey(s => s.PreviousSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_subscriptions_previous_subscription_id");

        builder.Property(s => s.PlanId)
            .HasColumnName("plan_id")
            .IsRequired();

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_subscriptions_subscription_plans_plan_id");

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<byte>()
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(s => s.CurrentPeriodStart)
            .HasColumnName("current_period_start")
            .IsRequired();

        builder.Property(s => s.CurrentPeriodEnd)
            .HasColumnName("current_period_end")
            .IsRequired();

        builder.Property(s => s.TrialEndsAt)
            .HasColumnName("trial_ends_at");

        builder.Property(s => s.CancelledAt)
            .HasColumnName("cancelled_at");


        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(s => s.CreatedOnUtc)
            .HasColumnName("created_on_utc")
            .IsRequired();

        builder.Property(s => s.IsDeleted)
            .HasColumnName("is_deleted");

        builder.Property(s => s.DeletedOnUtc)
            .HasColumnName("deleted_on_utc");

        builder.Property(s => s.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.HasIndex(s => s.TenantId)
            .HasDatabaseName("ix_subscriptions_tenant_id");

        builder.HasIndex(s => new { s.TenantId, s.DoctorId, s.Status })
            .HasDatabaseName("ix_subscriptions_tenant_id_doctor_id_status");

        builder.Navigation(s => s.Payments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
