using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.Infrastructure.Persistence;

namespace Modules.Employees.Infrastructure.Presistance;

public sealed class RotationEntryConfiguration : IEntityTypeConfiguration<RotationEntry>
{
    public void Configure(EntityTypeBuilder<RotationEntry> builder)
    {
        builder.ToTable("rotation_entries", "employees");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => new RotationEntryId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.EmployeeGroupId)
            .HasColumnName("employee_group_id")
            .HasConversion(
                id => id.Value,
                value => new EmployeeGroupId(value))
            .IsRequired();

        builder.Property(x => x.Position)
            .HasColumnName("position")
            .IsRequired();

        builder.Property(x => x.WorkScheduleId)
            .HasColumnName("work_schedule_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new WorkScheduleId(value.Value) : (WorkScheduleId?)null)
            .IsRequired(false);

        builder.Property(x => x.CreatedOnUtc)
            .HasColumnName("created_on_utc")
            .IsRequired();

        builder.HasOne(x => x.EmployeeGroup)
            .WithMany(x => x.RotationEntries)
            .HasForeignKey(x => x.EmployeeGroupId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_rotation_entries_employee_groups_employee_group_id");

        builder.HasOne(x => x.WorkSchedule)
            .WithMany()
            .HasForeignKey(x => x.WorkScheduleId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_rotation_entries_work_schedules_work_schedule_id");

        builder.HasIndex(x => new { x.EmployeeGroupId, x.Position })
            .IsUnique()
            .HasDatabaseName("ix_rotation_entries_employee_group_id_position");

        builder.Navigation(x => x.WorkSchedule)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}