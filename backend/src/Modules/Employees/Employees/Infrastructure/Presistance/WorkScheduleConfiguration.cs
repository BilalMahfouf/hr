using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Shared.Infrastructure.Persistence;

namespace Modules.Employees.Infrastructure.Presistance;

public sealed class WorkScheduleConfiguration
    : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> builder)
    {
        builder.ToTable("work_schedules");

        builder.IgnoreSoftDelete<WorkSchedule>();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => new WorkScheduleId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.EmployeeGroupId)
            .HasColumnName("employee_group_id")
            .HasConversion(
                id => id.Value,
                value => new EmployeeGroupId(value))
            .IsRequired();

        builder.Property(x => x.ShiftStartTime)
            .HasColumnName("shift_start_time")
            .IsRequired();

        builder.Property(x => x.ShiftEndTime)
            .HasColumnName("shift_end_time")
            .IsRequired();

        builder.Property(x => x.BreakStartTime)
            .HasColumnName("break_start_time")
            .IsRequired();

        builder.Property(x => x.BreakEndTime)
            .HasColumnName("break_end_time")
            .IsRequired();

        builder.Property(x => x.AllowedCheckInLatenessMinutes)
            .HasColumnName("allowed_check_in_lateness_minutes")
            .IsRequired();

        builder.Property(x => x.AllowedCheckOutEarlinessMinutes)
            .HasColumnName("allowed_check_out_earliness_minutes")
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .HasColumnName("created_on_utc")
            .IsRequired();

        builder.HasOne(x => x.EmployeeGroup)
            .WithMany(x => x.WorkSchedules)
            .HasForeignKey(x => x.EmployeeGroupId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_work_schedules_employee_groups_employee_group_id");
    }
}