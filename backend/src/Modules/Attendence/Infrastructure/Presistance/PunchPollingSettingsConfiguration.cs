using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Attendence.Domain.PunchPolling;
using Modules.Shared.Infrastructure.Persistence;

namespace Modules.Attendence.Infrastructure.Presistance;

public sealed class PunchPollingSettingsConfiguration
    : IEntityTypeConfiguration<PunchPollingSettings>
{
    public void Configure(EntityTypeBuilder<PunchPollingSettings> builder)
    {
        builder.ToTable("punch_polling_settings");

        builder.IgnoreSoftDelete<PunchPollingSettings>();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => new PunchPollingSettingsId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.Property(x => x.IntervalMinutes)
            .HasColumnName("interval_minutes")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
