using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Domain.Punches;

public readonly record struct PunchId(Guid Value)
{
    public static PunchId New() => new(Guid.CreateVersion7());

    public static PunchId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Punch id cannot be empty.", nameof(value));

        return new(value);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(PunchId id) => id.Value;

    public static explicit operator PunchId(Guid value) => From(value);
}
