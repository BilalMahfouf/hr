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
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => new MachineId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.MachineNumber)
            .HasColumnName("machine_number")
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .IsRequired();

        builder.Property(x => x.Port)
            .HasColumnName("port")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .IsRequired()
            .HasMaxLength(50);
    }
}