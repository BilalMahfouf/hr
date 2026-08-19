using Modules.Shared.Domain.Common;
using System;

namespace Modules.Attendence.Domain.AttendenceRecords;

public sealed class AttendanceRecord : Entity
{
    public static readonly int MinMinutesBetweenCheckInAndCheckOut = 3;

    public new AttendanceRecordId Id { get; private set; }

    public MachineId MachineId { get; private set; }

    // Reference to Employee module
    public string EmployeeId { get; private set; } = null!;

    public DateTime CheckInAt { get; private set; }

    public DateTime? CheckOutAt { get; private set; }

    public TimeSpan WorkedTime { get; private set; } = TimeSpan.Zero;

    public TimeSpan Overtime { get; private set; } = TimeSpan.Zero;

    public TimeSpan LateTime { get; private set; } = TimeSpan.Zero;

    public TimeSpan EarlyLeaveTime { get; private set; } = TimeSpan.Zero;

    public bool IsAbsent { get; private set; } = false;

    private AttendanceRecord() { }

    private AttendanceRecord(
        AttendanceRecordId id,
        MachineId machineId,
        string employeeId)
    {
        Id = id;
        MachineId = machineId;
        EmployeeId = employeeId;

        WorkedTime = TimeSpan.Zero;
        Overtime = TimeSpan.Zero;
        LateTime = TimeSpan.Zero;
        EarlyLeaveTime = TimeSpan.Zero;
        IsAbsent = false;
    }

    public static AttendanceRecord Create(
        MachineId machineId,
        string employeeId)
    {
        return new AttendanceRecord(
            AttendanceRecordId.New(),
            machineId,
            employeeId);
    }

    public void RegisterCheckIn(
        DateTime checkInAt,
        DateTime expectedCheckInTime,
        AttendanceRecord? prevRecord)
    {
        if (prevRecord?.CheckOutAt is DateTime previousCheckOutAt &&
            previousCheckOutAt.AddMinutes(MinMinutesBetweenCheckInAndCheckOut) >= checkInAt)
        {
            throw new DomainException(AttendanceRecordErrors.InvalidAttendanceTimeRange);
        }

        CheckInAt = checkInAt;
        CalculateLateTime(expectedCheckInTime);
    }

    public void RegisterCheckOut(DateTime checkOutAt, WorkSchedule workSchedule)
    {
        if (this.CheckInAt >= checkOutAt)
        {
            throw new DomainException(AttendanceRecordErrors.InvalidAttendanceTimeRange);
        }
        if (this.CheckInAt.AddMinutes(MinMinutesBetweenCheckInAndCheckOut) >= checkOutAt)
        {
            throw new DomainException(AttendanceRecordErrors.InvalidAttendanceTimeRange);
        }
        CheckOutAt = checkOutAt;
        CalculateWorkedTime();
        CalculateOvertime(workSchedule.StandardWorkTime);
        CalculateEarlyLeave(workSchedule.ExpectedCheckOutTime);
    }

    private void CalculateWorkedTime()
    {
        if (CheckOutAt is null)
            return;

        var duration = CheckOutAt.Value - CheckInAt;

        WorkedTime = duration;
    }

    private void CalculateOvertime(TimeSpan standardWorkTime)
    {
        if (WorkedTime > standardWorkTime)
        {
            Overtime = WorkedTime - standardWorkTime;
        }
        else
        {
            Overtime = TimeSpan.Zero;
        }
    }

    private void CalculateLateTime(DateTime expectedCheckInAt)
    {
        var lateTime = CheckInAt - expectedCheckInAt;

        LateTime = lateTime > TimeSpan.Zero
            ? lateTime
            : TimeSpan.Zero;
    }

    private void CalculateEarlyLeave(DateTime expectedCheckOutAt)
    {
        if (CheckOutAt is null)
            return;

        var earlyLeave = expectedCheckOutAt - CheckOutAt.Value;

        EarlyLeaveTime = earlyLeave > TimeSpan.Zero
            ? earlyLeave
            : TimeSpan.Zero;
    }

    public void MarkAsAbsent()
    {
        IsAbsent = true;
    }
}