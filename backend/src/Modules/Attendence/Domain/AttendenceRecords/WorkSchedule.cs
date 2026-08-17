using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Domain.AttendenceRecords;

public sealed record WorkSchedule(
    TimeSpan StandardWorkTime,
    DateTime ExpectedCheckOutTime);
