using Modules.Shared.Errors;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Domain.AttendenceRecords;

public static class AttendanceRecordErrors
{
    public static Error InvalidAttendanceTimeRange =>
        Error.Conflict($"{nameof(AttendanceRecord)}.${nameof(InvalidAttendanceTimeRange)}",
            "Check-out time must be after check-in time.");
}
