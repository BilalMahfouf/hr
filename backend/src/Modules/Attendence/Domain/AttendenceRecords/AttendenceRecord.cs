using Modules.Shared.Domain.Common;
using System;

namespace Modules.Attendence.Domain.AttendenceRecords;

public sealed class AttendanceRecord : Entity
{
    public new AttendanceRecordId Id { get; private set; }


    public MachineId MachineId { get; private set; }

    // Reference to Employee module
    public string EmployeeId { get; private set; } = null!;

    public DateTime PunchDate { get; private set; }

    public DateTime? CheckInAt { get; private set; }

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
        string employeeId,
        DateTime punchDate)
    {
        Id = id;
        MachineId = machineId;
        EmployeeId = employeeId;
        PunchDate = punchDate;

        WorkedTime = TimeSpan.Zero;
        Overtime = TimeSpan.Zero;
        LateTime = TimeSpan.Zero;
        EarlyLeaveTime = TimeSpan.Zero;
        IsAbsent = false;
    }

    public static AttendanceRecord Create(
        MachineId machineId,
        string employeeId,
        DateTime punchDate)
    {
        return new AttendanceRecord(
            AttendanceRecordId.New(),
            machineId,
            employeeId,
            punchDate);
    }

    public void RegisterCheckIn(DateTime checkInAt, DateTime ExpectedCheckInTime)
    {
        CheckInAt = checkInAt;
        CalculateLateTime(ExpectedCheckInTime);
    }

    public void RegisterCheckOut(DateTime checkOutAt, WorkSchedule workSchedule)
    {
        if (this.CheckInAt >= checkOutAt)
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
        if (CheckInAt is null || CheckOutAt is null)
            return;

        var duration = CheckOutAt.Value - CheckInAt.Value;

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
        if (CheckInAt is null)
            return;

        var lateTime = CheckInAt.Value - expectedCheckInAt;

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