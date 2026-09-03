using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Attendence.Domain.Punches;
using Modules.Shared.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Infrastructure.Presistance
{
    public sealed class PunchConfiguration : IEntityTypeConfiguration<Punch>
    {
        public void Configure(EntityTypeBuilder<Punch> builder)
        {
            builder.ToTable("punches");

             
            builder.IgnoreSoftDelete<Punch>();

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasConversion(
                    id => id.Value,
                    value => new PunchId(value))
                .ValueGeneratedNever();

            builder.Property(x => x.MachineId)
                .HasColumnName("machine_id")
                .HasConversion(
                    id => id.Value,
                    value => new MachineId(value))
                .IsRequired();

            builder.Property(x => x.EmployeeBadge)
                .HasColumnName("employee_badge")
                .IsRequired();

            builder.Property(x => x.PunchOccurredAt)
                .HasColumnName("punch_occurred_at")
                .IsRequired();

            builder.Property(x => x.CreatedOnUtc)
                .HasColumnName("created_on_utc")
                .IsRequired();

            // Optional but highly recommended for attendance ingestion
            builder.HasIndex(x => new
            {
                x.MachineId,
                x.EmployeeBadge,
                x.PunchOccurredAt
            });

        }
    }

}
