using Modules.Shared.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Domain.AttendenceRecords;

public static class AttendanceRecordErrors
{
    public static Error EmployeeIsOnRestDay =>
        Error.Conflict($"{nameof(AttendanceRecord)}.${nameof(EmployeeIsOnRestDay)}",
            "Employee is on a rest day.");
    public static Error InvalidAttendanceTimeRange =>
        Error.Conflict($"{nameof(AttendanceRecord)}.${nameof(InvalidAttendanceTimeRange)}",
            "Check-out time must be after check-in time.");

    public static Error RecordNotFound(Guid id) =>
        Error.NotFound(
            $"{nameof(AttendanceRecord)}.NotFound",
            $"Attendance record with id {id} is not found");

    public static Error RecordsNotFound =>
        Error.NotFound(
            $"{nameof(AttendanceRecord)}.RecordsNotFound",
            "No attendance records were found");
}
