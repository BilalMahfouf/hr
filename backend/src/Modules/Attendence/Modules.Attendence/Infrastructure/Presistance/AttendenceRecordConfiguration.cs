using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Attendence.Domain.AttendenceRecords;


namespace Modules.Attendence.Infrastructure.Presistance; 


public sealed class AttendanceRecordConfiguration
    : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => new AttendanceRecordId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.MachineId)
            .HasConversion(
                id => id.Value,
                value => new MachineId(value))
            .IsRequired();

        // Cross-module reference.
        // No FK/navigation to Employee module.
        builder.Property(x => x.EmployeeId)
            .IsRequired();

        builder.Property(x => x.PunchDate)
            .IsRequired();

        builder.Property(x => x.CheckInAt);

        builder.Property(x => x.CheckOutAt);

        builder.Property(x => x.WorkedTime)
            .IsRequired();

        builder.Property(x => x.Overtime)
            .IsRequired();

        builder.Property(x => x.LateTime)
            .IsRequired();

        builder.Property(x => x.EarlyLeaveTime)
            .IsRequired();

        builder.Property(x => x.IsAbsent)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.EmployeeId,
            x.PunchDate
        });
    }
}
