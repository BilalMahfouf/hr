using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Shared.Infrastructure.Persistence;


namespace Modules.Attendence.Infrastructure.Presistance; 


public sealed class AttendanceRecordConfiguration
    : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records");

        builder.IgnoreSoftDelete<AttendanceRecord>();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => new AttendanceRecordId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.MachineId)
            .HasColumnName("machine_id")
            .HasConversion(
                id => id.Value,
                value => new MachineId(value))
            .IsRequired();

        // Cross-module reference.
        // No FK/navigation to Employee module.
        builder.Property(x => x.EmployeeId)
            .HasColumnName("employee_id")
            .IsRequired();

        builder.Property(x => x.CheckInAt)
            .HasColumnName("check_in_at");

        builder.Property(x => x.CheckOutAt)
            .HasColumnName("check_out_at");

        builder.Property(x => x.WorkedTime)
            .HasColumnName("worked_time")
            .IsRequired();

        builder.Property(x => x.Overtime)
            .HasColumnName("overtime")
            .IsRequired();

        builder.Property(x => x.LateTime)
            .HasColumnName("late_time")
            .IsRequired();

        builder.Property(x => x.EarlyLeaveTime)
            .HasColumnName("early_leave_time")
            .IsRequired();

        builder.Property(x => x.IsAbsent)
            .HasColumnName("is_absent")
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.EmployeeId,
            x.CheckInAt
        });
    }
}
