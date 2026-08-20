using System;

namespace Modules.Employees.Domain.EmployeeGroups;

public readonly record struct EmployeeGroupId(Guid Value)
{
    public static EmployeeGroupId New() => new(Guid.CreateVersion7());

    public static EmployeeGroupId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Employee group id cannot be empty.", nameof(value));

        return new(value);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(EmployeeGroupId id) => id.Value;

    public static explicit operator EmployeeGroupId(Guid value) => From(value);
}