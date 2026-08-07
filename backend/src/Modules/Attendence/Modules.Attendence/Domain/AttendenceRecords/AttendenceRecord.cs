using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Attendence.Domain.AttendenceRecords;

public sealed class AttendanceRecord
{
    public AttendanceRecordId Id { get; private set; }

    public MachineId MachineId { get; private set; }

    // Reference to Employee module
    public string EmployeeId { get; private set; } = null!;

    public DateTime PunchDate { get; private set; }

    public DateTime? CheckInAt { get; private set; }

    public DateTime? CheckOutAt { get; private set; }

    public decimal WorkedMinutes { get; private set; }

    public decimal OvertimeMinutes { get; private set; }

    public decimal LateMinutes { get; private set; }

    public decimal EarlyLeaveMinutes { get; private set; }

    public bool IsAbsent { get; private set; }

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

        WorkedMinutes = 0;
        OvertimeMinutes = 0;
        LateMinutes = 0;
        EarlyLeaveMinutes = 0;
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

    public void RegisterCheckIn(DateTime checkInAt)
    {
        CheckInAt = checkInAt;
    }

    public void RegisterCheckOut(DateTime checkOutAt)
    {
        CheckOutAt = checkOutAt;
    }

    public void SetAttendanceSummary(
        decimal workedMinutes,
        decimal overtimeMinutes,
        decimal lateMinutes,
        decimal earlyLeaveMinutes,
        bool isAbsent)
    {
        WorkedMinutes = workedMinutes;
        OvertimeMinutes = overtimeMinutes;
        LateMinutes = lateMinutes;
        EarlyLeaveMinutes = earlyLeaveMinutes;
        IsAbsent = isAbsent;
    }
}
