using System;
using System.Collections.Generic;
using System.Text;

public readonly record struct MachineId(Guid Value)
{
    public static MachineId New() => new(Guid.CreateVersion7());

    public static MachineId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Machine id cannot be empty.", nameof(value));

        return new(value);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(MachineId id) => id.Value;

    public static explicit operator MachineId(Guid value) => From(value);
}
