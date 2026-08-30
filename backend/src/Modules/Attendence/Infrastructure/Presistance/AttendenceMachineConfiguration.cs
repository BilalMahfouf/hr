using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Attendence.Domain.Machines;
using Modules.Shared.Infrastructure.Persistence;

namespace Modules.Attendence.Infrastructure.Presistance;

public sealed class AttendenceMachineConfiguration
    : IEntityTypeConfiguration<AttendenceMachine>
{
    public void Configure(EntityTypeBuilder<AttendenceMachine> builder)
    {
        builder.ToTable("machines");

        builder.IgnoreSoftDelete<AttendenceMachine>();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new MachineId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.MachineNumber)
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .IsRequired();

        builder.Property(x => x.Port)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(50);
    }
}