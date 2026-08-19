using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Domain.AttendenceRecords;



public readonly record struct AttendanceRecordId(Guid Value)
{
    public static AttendanceRecordId New() => new(Guid.NewGuid());

    public static AttendanceRecordId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(AttendanceRecordId id) => id.Value;

    public static explicit operator AttendanceRecordId(Guid value) => new(value);
}
