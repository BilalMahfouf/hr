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
            .HasConversion(
                id => id.Value,
                value => new PunchPollingSettingsId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.IntervalMinutes)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}
