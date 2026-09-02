using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Shared.Infrastructure.Persistence;

namespace Modules.Employees.Infrastructure.Presistance;

public sealed class EmployeeGroupConfiguration
    : IEntityTypeConfiguration<EmployeeGroup>
{
    public void Configure(EntityTypeBuilder<EmployeeGroup> builder)
    {
        builder.ToTable("employee_groups");

        builder.IgnoreSoftDelete<EmployeeGroup>();

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => new EmployeeGroupId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(x => x.GroupNumber)
            .HasColumnName("group_number")
            .IsRequired();

        builder.Ignore(x => x.NumberOfRotations);

        builder.Property(x => x.RotationStartDate)
            .HasColumnName("rotation_start_date")
            .IsRequired();

        builder.Property(x => x.IsSecurity)
            .HasColumnName("is_security")
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description");

        builder.Property(x => x.CreatedOnUtc)
            .HasColumnName("created_on_utc")
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("ix_employee_groups_name");

        builder.HasIndex(x => x.GroupNumber)
            .IsUnique()
            .HasDatabaseName("ix_employee_groups_group_number");

        builder.Navigation(x => x.WorkSchedules)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.RotationEntries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}