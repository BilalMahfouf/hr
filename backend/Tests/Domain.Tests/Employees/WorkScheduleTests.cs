using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.Domain.Common;

namespace Domain.Tests.Employees;

public sealed class WorkScheduleTests
{
    private static readonly EmployeeGroupId EmployeeGroupId = EmployeeGroupId.New();

    private static WorkSchedule CreateSchedule() =>
        WorkSchedule.Create(
            EmployeeGroupId,
            new TimeOnly(8, 0),
            new TimeOnly(16, 0),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            allowedCheckInLatenessMinutes: 5,
            allowedCheckOutEarlinessMinutes: 5);

    [Fact]
    public void Create_SetsExpectedInitialState()
    {
        var schedule = CreateSchedule();

        Assert.NotEqual(Guid.Empty, schedule.Id.Value);
        Assert.Equal(EmployeeGroupId, schedule.EmployeeGroupId);
        Assert.Equal(new TimeOnly(8, 0), schedule.ShiftStartTime);
        Assert.Equal(new TimeOnly(16, 0), schedule.ShiftEndTime);
        Assert.Equal(new TimeOnly(12, 0), schedule.BreakStartTime);
        Assert.Equal(new TimeOnly(13, 0), schedule.BreakEndTime);
        Assert.Equal(5, schedule.AllowedCheckInLatenessMinutes);
        Assert.Equal(5, schedule.AllowedCheckOutEarlinessMinutes);
    }

    [Fact]
    public void Create_WhenShiftStartNotBeforeShiftEnd_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            WorkSchedule.Create(
                EmployeeGroupId,
                new TimeOnly(16, 0),
                new TimeOnly(16, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                0, 0));

        Assert.Equal(WorkScheduleErrors.InvalidShiftRange.Code, exception.Error.Code);
    }

    [Fact]
    public void Create_WhenBreakStartNotBeforeBreakEnd_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            WorkSchedule.Create(
                EmployeeGroupId,
                new TimeOnly(8, 0),
                new TimeOnly(16, 0),
                new TimeOnly(13, 0),
                new TimeOnly(12, 0),
                0, 0));

        Assert.Equal(WorkScheduleErrors.InvalidBreakRange.Code, exception.Error.Code);
    }

    [Fact]
    public void Create_WhenBreakStartsBeforeShift_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            WorkSchedule.Create(
                EmployeeGroupId,
                new TimeOnly(8, 0),
                new TimeOnly(16, 0),
                new TimeOnly(7, 0),
                new TimeOnly(9, 0),
                0, 0));

        Assert.Equal(WorkScheduleErrors.BreakOutsideShift.Code, exception.Error.Code);
    }

    [Fact]
    public void Create_WhenBreakEndsAfterShift_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            WorkSchedule.Create(
                EmployeeGroupId,
                new TimeOnly(8, 0),
                new TimeOnly(16, 0),
                new TimeOnly(15, 0),
                new TimeOnly(17, 0),
                0, 0));

        Assert.Equal(WorkScheduleErrors.BreakOutsideShift.Code, exception.Error.Code);
    }

    [Fact]
    public void Create_WhenCheckInLatenessNegative_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            WorkSchedule.Create(
                EmployeeGroupId,
                new TimeOnly(8, 0),
                new TimeOnly(16, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                -1, 0));

        Assert.Equal(WorkScheduleErrors.InvalidCheckInLateness.Code, exception.Error.Code);
    }

    [Fact]
    public void Create_WhenCheckOutEarlinessNegative_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            WorkSchedule.Create(
                EmployeeGroupId,
                new TimeOnly(8, 0),
                new TimeOnly(16, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                0, -1));

        Assert.Equal(WorkScheduleErrors.InvalidCheckOutEarliness.Code, exception.Error.Code);
    }
}