using Modules.Shared.Domain.Common;
using System;

namespace Modules.Employees.Domain.EmployeeGroups;

public sealed class WorkSchedule : Entity
{
    public new WorkScheduleId Id { get; private set; }

    public EmployeeGroupId EmployeeGroupId { get; private set; }

    public TimeOnly ShiftStartTime { get; private set; }

    public TimeOnly ShiftEndTime { get; private set; }

    public TimeOnly BreakStartTime { get; private set; }

    public TimeOnly BreakEndTime { get; private set; }

    public int AllowedCheckInLatenessMinutes { get; private set; }

    public int AllowedCheckOutEarlinessMinutes { get; private set; }

    public EmployeeGroup EmployeeGroup { get; private set; } = null!;

    private WorkSchedule()
    {
    }

    private WorkSchedule(
        WorkScheduleId id,
        EmployeeGroupId employeeGroupId,
        TimeOnly shiftStartTime,
        TimeOnly shiftEndTime,
        TimeOnly breakStartTime,
        TimeOnly breakEndTime,
        int allowedCheckInLatenessMinutes,
        int allowedCheckOutEarlinessMinutes)
    {
        Id = id;
        EmployeeGroupId = employeeGroupId;
        ShiftStartTime = shiftStartTime;
        ShiftEndTime = shiftEndTime;
        BreakStartTime = breakStartTime;
        BreakEndTime = breakEndTime;
        AllowedCheckInLatenessMinutes = allowedCheckInLatenessMinutes;
        AllowedCheckOutEarlinessMinutes = allowedCheckOutEarlinessMinutes;
    }

    public static WorkSchedule Create(
        EmployeeGroupId employeeGroupId,
        TimeOnly shiftStartTime,
        TimeOnly shiftEndTime,
        TimeOnly breakStartTime,
        TimeOnly breakEndTime,
        int allowedCheckInLatenessMinutes,
        int allowedCheckOutEarlinessMinutes)
    {
        if (shiftStartTime >= shiftEndTime)
            throw new DomainException(WorkScheduleErrors.InvalidShiftRange);

        if (breakStartTime >= breakEndTime)
            throw new DomainException(WorkScheduleErrors.InvalidBreakRange);

        if (breakStartTime < shiftStartTime || breakEndTime > shiftEndTime)
            throw new DomainException(WorkScheduleErrors.BreakOutsideShift);

        if (allowedCheckInLatenessMinutes < 0)
            throw new DomainException(WorkScheduleErrors.InvalidCheckInLateness);

        if (allowedCheckOutEarlinessMinutes < 0)
            throw new DomainException(WorkScheduleErrors.InvalidCheckOutEarliness);

        return new WorkSchedule(
            WorkScheduleId.New(),
            employeeGroupId,
            shiftStartTime,
            shiftEndTime,
            breakStartTime,
            breakEndTime,
            allowedCheckInLatenessMinutes,
            allowedCheckOutEarlinessMinutes);
    }
}