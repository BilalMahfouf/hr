using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Employees.Domain.EmployeeGroups.WorkSchedules;

public sealed record CreateWorkScheduleDto(
    EmployeeGroupId EmployeeGroupId,
    TimeOnly ShiftStartTime,
    TimeOnly ShiftEndTime,
    int EndDayOffset,
    TimeOnly BreakStartTime,
    TimeOnly BreakEndTime,
    int AllowedCheckInLatenessMinutes,
    int AllowedCheckOutEarlinessMinutes
);
public sealed record UpdateWorkScheduleDto(
    WorkScheduleId Id,
    EmployeeGroupId EmployeeGroupId,
    TimeOnly ShiftStartTime,
    TimeOnly ShiftEndTime,
    int EndDayOffset,
    TimeOnly BreakStartTime,
    TimeOnly BreakEndTime,
    int AllowedCheckInLatenessMinutes,
    int AllowedCheckOutEarlinessMinutes
);
