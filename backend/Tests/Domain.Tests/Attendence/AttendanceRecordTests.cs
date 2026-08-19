using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Shared.Domain.Common;

namespace Domain.Tests.Attendence;

public sealed class AttendanceRecordTests
{
    private static readonly MachineId MachineId = MachineId.New();

    private const string EmployeeId = "emp-1";

    private static AttendanceRecord CreateRecord() =>
        AttendanceRecord.Create(MachineId, EmployeeId);

    [Fact]
    public void Create_SetsExpectedInitialState()
    {
        var record = CreateRecord();

        Assert.NotEqual(AttendanceRecordId.Empty, record.Id);
        Assert.Equal(MachineId, record.MachineId);
        Assert.Equal(EmployeeId, record.EmployeeId);
        Assert.Equal(default, record.CheckInAt);
        Assert.Null(record.CheckOutAt);
        Assert.Equal(TimeSpan.Zero, record.WorkedTime);
        Assert.Equal(TimeSpan.Zero, record.Overtime);
        Assert.Equal(TimeSpan.Zero, record.LateTime);
        Assert.Equal(TimeSpan.Zero, record.EarlyLeaveTime);
        Assert.False(record.IsAbsent);
    }

    [Fact]
    public void RegisterCheckIn_SetsCheckInTime()
    {
        var record = CreateRecord();
        var checkIn = new DateTime(2026, 8, 13, 9, 0, 0);

        record.RegisterCheckIn(checkIn, checkIn, null);

        Assert.Equal(checkIn, record.CheckInAt);
    }

    [Fact]
    public void RegisterCheckIn_WhenLate_SetsLateTime()
    {
        var record = CreateRecord();
        var expected = new DateTime(2026, 8, 13, 9, 0, 0);
        var actual = new DateTime(2026, 8, 13, 9, 15, 0);

        record.RegisterCheckIn(actual, expected, null);

        Assert.Equal(TimeSpan.FromMinutes(15), record.LateTime);
    }

    [Fact]
    public void RegisterCheckIn_WhenOnTime_LateTimeIsZero()
    {
        var record = CreateRecord();
        var expected = new DateTime(2026, 8, 13, 9, 0, 0);
        var actual = new DateTime(2026, 8, 13, 8, 30, 0);

        record.RegisterCheckIn(actual, expected, null);

        Assert.Equal(TimeSpan.Zero, record.LateTime);
    }

    [Fact]
    public void RegisterCheckOut_ComputesWorkedTimeAndOvertime()
    {
        var record = CreateRecord();
        record.RegisterCheckIn(new DateTime(2026, 8, 13, 8, 0, 0), new DateTime(2026, 8, 13, 9, 0, 0), null);

        record.RegisterCheckOut(
            new DateTime(2026, 8, 13, 18, 0, 0),
            new WorkSchedule(
                StandardWorkTime: TimeSpan.FromHours(8),
                ExpectedCheckOutTime: new DateTime(2026, 8, 13, 17, 0, 0)));

        Assert.Equal(TimeSpan.FromHours(10), record.WorkedTime);
        Assert.Equal(TimeSpan.FromHours(2), record.Overtime);
        Assert.Equal(TimeSpan.Zero, record.EarlyLeaveTime);
    }

    [Fact]
    public void RegisterCheckOut_WhenWorkedWithinStandard_OvertimeIsZero()
    {
        var record = CreateRecord();
        record.RegisterCheckIn(new DateTime(2026, 8, 13, 8, 0, 0), new DateTime(2026, 8, 13, 9, 0, 0), null);

        record.RegisterCheckOut(
            new DateTime(2026, 8, 13, 16, 0, 0),
            new WorkSchedule(
                StandardWorkTime: TimeSpan.FromHours(8),
                ExpectedCheckOutTime: new DateTime(2026, 8, 13, 17, 0, 0)));

        Assert.Equal(TimeSpan.FromHours(8), record.WorkedTime);
        Assert.Equal(TimeSpan.Zero, record.Overtime);
    }

    [Fact]
    public void RegisterCheckOut_WhenLeavesEarly_SetsEarlyLeaveTime()
    {
        var record = CreateRecord();
        record.RegisterCheckIn(new DateTime(2026, 8, 13, 8, 0, 0), new DateTime(2026, 8, 13, 9, 0, 0), null);

        record.RegisterCheckOut(
            new DateTime(2026, 8, 13, 15, 0, 0),
            new WorkSchedule(
                StandardWorkTime: TimeSpan.FromHours(8),
                ExpectedCheckOutTime: new DateTime(2026, 8, 13, 17, 0, 0)));

        Assert.Equal(TimeSpan.FromHours(7), record.WorkedTime);
        Assert.Equal(TimeSpan.Zero, record.Overtime);
        Assert.Equal(TimeSpan.FromHours(2), record.EarlyLeaveTime);
    }

    [Fact]
    public void MarkAsAbsent_SetsIsAbsent()
    {
        var record = CreateRecord();

        record.MarkAsAbsent();

        Assert.True(record.IsAbsent);
    }

    [Fact]
    public void RegisterCheckOut_WhenCheckOutEarlierThanCheckIn_ThrowsDomainException()
    {
        var record = CreateRecord();
        record.RegisterCheckIn(new DateTime(2026, 8, 13, 10, 0, 0), new DateTime(2026, 8, 13, 9, 0, 0), null);

        var exception = Assert.Throws<DomainException>(() =>
            record.RegisterCheckOut(
                new DateTime(2026, 8, 13, 8, 0, 0),
                new WorkSchedule(
                    StandardWorkTime: TimeSpan.FromHours(8),
                    ExpectedCheckOutTime: new DateTime(2026, 8, 13, 17, 0, 0))));

        Assert.Equal(AttendanceRecordErrors.InvalidAttendanceTimeRange.Code, exception.Error.Code);
        Assert.Null(record.CheckOutAt);
        Assert.Equal(TimeSpan.Zero, record.WorkedTime);
    }
}