using System;

namespace Modules.Employees.Domain.EmployeeGroups.WorkSchedules;

public readonly record struct WorkScheduleId(Guid Value)
{
    public static WorkScheduleId New() => new(Guid.CreateVersion7());

    public static WorkScheduleId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Work schedule id cannot be empty.", nameof(value));

        return new(value);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(WorkScheduleId id) => id.Value;

    public static explicit operator WorkScheduleId(Guid value) => From(value);
}