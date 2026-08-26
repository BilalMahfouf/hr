using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Employees.Domain.EmployeeGroups.Rotation;


public sealed class RotationEntry : Entity
{
    public new RotationEntryId Id { get; private set; }

    public EmployeeGroupId EmployeeGroupId { get; private set; }

    public int Position { get; private set; }

    public RotationStatus Status => WorkScheduleId is null ? RotationStatus.Rest : RotationStatus.Work;

    public WorkScheduleId? WorkScheduleId { get; private set; }

    // Navigation
    public EmployeeGroup EmployeeGroup { get; private set; } = null!;

    public WorkSchedule? WorkSchedule { get; private set; }

    public static RotationEntry Create(
        EmployeeGroupId employeeGroupId,
        int position,
        WorkScheduleId? workScheduleId)
    {
        if (position < 1)
            throw new DomainException(RotationEntryErrors.InvalidPosition);
        return new RotationEntry
        {
            Id = RotationEntryId.New(),
            EmployeeGroupId = employeeGroupId,
            Position = position,
            WorkScheduleId = workScheduleId
        };
    }
}

public enum RotationStatus
{
    Work = 1,
    Rest = 2,
}

public readonly record struct RotationEntryId(Guid Value)
{
    public static RotationEntryId New() => new(Guid.CreateVersion7());

    public static RotationEntryId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Rotation entry id cannot be empty.", nameof(value));

        return new(value);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(RotationEntryId id) => id.Value;

    public static explicit operator RotationEntryId(Guid value) => From(value);
}
