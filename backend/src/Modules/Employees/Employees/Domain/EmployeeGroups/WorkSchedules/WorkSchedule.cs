using Modules.Shared.Domain.Common;
using System;

namespace Modules.Employees.Domain.EmployeeGroups.WorkSchedules;

public sealed class WorkSchedule : Entity
{
    public new WorkScheduleId Id { get; private set; }

    public EmployeeGroupId EmployeeGroupId { get; private set; }

    public TimeOnly ShiftStartTime { get; private set; }

    public TimeOnly ShiftEndTime { get; private set; }

    public TimeSpan WorkTime => CalculateWorkTime();

    public int EndDayOffset { get; private set; }

    public TimeOnly BreakStartTime { get; private set; }

    public TimeOnly BreakEndTime { get; private set; }

    public int AllowedCheckInLatenessMinutes { get; private set; }

    public int AllowedCheckOutEarlinessMinutes { get; private set; }

    public bool IsActive { get; private set; }

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
        int allowedCheckOutEarlinessMinutes,
        int endDayOffset)
    {
        Id = id;
        EmployeeGroupId = employeeGroupId;
        ShiftStartTime = shiftStartTime;
        ShiftEndTime = shiftEndTime;
        BreakStartTime = breakStartTime;
        BreakEndTime = breakEndTime;
        AllowedCheckInLatenessMinutes = allowedCheckInLatenessMinutes;
        AllowedCheckOutEarlinessMinutes = allowedCheckOutEarlinessMinutes;
        EndDayOffset = endDayOffset;
        IsActive = false;
    }

    public static WorkSchedule Create(
        EmployeeGroupId employeeGroupId,
        TimeOnly shiftStartTime,
        TimeOnly shiftEndTime,
        TimeOnly breakStartTime,
        TimeOnly breakEndTime,
        int allowedCheckInLatenessMinutes,
        int allowedCheckOutEarlinessMinutes,
        int endDayOffset = 0)
    {
        if (shiftStartTime >= shiftEndTime && endDayOffset == 0)
            throw new DomainException(WorkScheduleErrors.InvalidShiftRange);

        if (breakStartTime >= breakEndTime && endDayOffset == 0)
            throw new DomainException(WorkScheduleErrors.InvalidBreakRange);

        if ((breakStartTime < shiftStartTime && endDayOffset == 0) || (breakEndTime > shiftEndTime && endDayOffset == 0))
            throw new DomainException(WorkScheduleErrors.BreakOutsideShift);

        if (allowedCheckInLatenessMinutes < 0)
            throw new DomainException(WorkScheduleErrors.InvalidCheckInLateness);

        if (allowedCheckOutEarlinessMinutes < 0)
            throw new DomainException(WorkScheduleErrors.InvalidCheckOutEarliness);

        WorkSchedule workSchedule = new WorkSchedule(
            WorkScheduleId.New(),
            employeeGroupId,
            shiftStartTime,
            shiftEndTime,
            breakStartTime,
            breakEndTime,
            allowedCheckInLatenessMinutes,
            allowedCheckOutEarlinessMinutes,
            endDayOffset);

        return workSchedule;
    }
    public void Activate()
    {
        RaiseDomainEvent(new WorkSheduleActivatedDomainEvent(Id, EmployeeGroupId, DateTime.UtcNow));
        IsActive = true;
    }
    public void Deactivate()
    {
        RaiseDomainEvent(new WorkSheduleDeactivatedDomainEvent(Id, EmployeeGroupId, DateTime.UtcNow));
        IsActive = false;
    }
    private TimeSpan CalculateWorkTime()
    {
        var today = DateTime.UtcNow;
        var workTime = today.Add(ShiftEndTime.ToTimeSpan()).AddDays(EndDayOffset) - today.Add(ShiftStartTime.ToTimeSpan());
        return workTime;
    }
    public void Update(
    TimeOnly shiftStartTime,
    TimeOnly shiftEndTime,
    TimeOnly breakStartTime,
    TimeOnly breakEndTime,
    int allowedCheckInLatenessMinutes,
    int allowedCheckOutEarlinessMinutes,
    int endDayOffset)
    {
        if (allowedCheckInLatenessMinutes < 0)
            throw new DomainException(
                WorkScheduleErrors.InvalidCheckInLateness);

        if (allowedCheckOutEarlinessMinutes < 0)
            throw new DomainException(
                WorkScheduleErrors.InvalidCheckOutEarliness);

        if (endDayOffset < 0)
            throw new DomainException(
                WorkScheduleErrors.InvalidEndDayOffset);

        ShiftStartTime = shiftStartTime;
        ShiftEndTime = shiftEndTime;
        BreakStartTime = breakStartTime;
        BreakEndTime = breakEndTime;
        AllowedCheckInLatenessMinutes = allowedCheckInLatenessMinutes;
        AllowedCheckOutEarlinessMinutes = allowedCheckOutEarlinessMinutes;
        EndDayOffset = endDayOffset;
    }

}