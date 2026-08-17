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
                .HasConversion(
                    id => id.Value,
                    value => new PunchId(value))
                .ValueGeneratedNever();

            builder.Property(x => x.MachineId)
                .HasConversion(
                    id => id.Value,
                    value => new MachineId(value))
                .IsRequired();

            builder.Property(x => x.EmployeeBadge)
                .IsRequired();

            builder.Property(x => x.PunchOccurredAt)
                .IsRequired();

            builder.Property(x => x.CreatedOnUtc)
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
