using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.Domain.Common;

namespace Domain.Tests.Employees;

public sealed class WorkScheduleTests
{
    private static readonly EmployeeGroupId EmployeeGroupId = EmployeeGroupId.New();

    private static WorkSchedule CreateSchedule(
        TimeOnly shiftStart = default, TimeOnly shiftEnd = default,
        TimeOnly breakStart = default, TimeOnly breakEnd = default,
        int allowedCheckInLatenessMinutes = 5,
        int allowedCheckOutEarlinessMinutes = 5,
        int endDayOffset = 0)
    {
        if (shiftStart == default) shiftStart = new TimeOnly(8, 0);
        if (shiftEnd == default) shiftEnd = new TimeOnly(16, 0);
        if (breakStart == default) breakStart = new TimeOnly(12, 0);
        if (breakEnd == default) breakEnd = new TimeOnly(13, 0);

        return WorkSchedule.Create(
            EmployeeGroupId,
            shiftStart,
            shiftEnd,
            breakStart,
            breakEnd,
            allowedCheckInLatenessMinutes,
            allowedCheckOutEarlinessMinutes,
            endDayOffset);
    }

    #region Create - Initial State

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
    public void Create_SetsIsActiveFalse()
    {
        var schedule = CreateSchedule();

        Assert.False(schedule.IsActive);
    }

    [Fact]
    public void Create_SetsEndDayOffset()
    {
        var schedule = CreateSchedule(endDayOffset: 1);

        Assert.Equal(1, schedule.EndDayOffset);
    }

    [Fact]
    public void Create_DefaultEndDayOffsetIsZero()
    {
        var schedule = CreateSchedule();

        Assert.Equal(0, schedule.EndDayOffset);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var schedule1 = CreateSchedule();
        var schedule2 = CreateSchedule();

        Assert.NotEqual(schedule1.Id, schedule2.Id);
    }

    #endregion

    #region Create - Shift Validation

    [Fact]
    public void Create_WhenShiftStartEqualsShiftEnd_ThrowsDomainException()
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
    public void Create_WhenShiftStartAfterShiftEnd_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            WorkSchedule.Create(
                EmployeeGroupId,
                new TimeOnly(17, 0),
                new TimeOnly(16, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                0, 0));

        Assert.Equal(WorkScheduleErrors.InvalidShiftRange.Code, exception.Error.Code);
    }

    [Fact]
    public void Create_CrossMidnight_ShiftWithEndDayOffsetAllowed()
    {
        var schedule = WorkSchedule.Create(
            EmployeeGroupId,
            new TimeOnly(22, 0),
            new TimeOnly(6, 0),
            new TimeOnly(23, 0),
            new TimeOnly(0, 0),
            0, 0,
            endDayOffset: 1);

        Assert.Equal(new TimeOnly(22, 0), schedule.ShiftStartTime);
        Assert.Equal(new TimeOnly(6, 0), schedule.ShiftEndTime);
        Assert.Equal(1, schedule.EndDayOffset);
    }

    #endregion

    #region Create - Break Validation

    [Fact]
    public void Create_WhenBreakStartEqualsBreakEnd_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            WorkSchedule.Create(
                EmployeeGroupId,
                new TimeOnly(8, 0),
                new TimeOnly(16, 0),
                new TimeOnly(12, 0),
                new TimeOnly(12, 0),
                0, 0));

        Assert.Equal(WorkScheduleErrors.InvalidBreakRange.Code, exception.Error.Code);
    }

    [Fact]
    public void Create_WhenBreakStartAfterBreakEnd_ThrowsDomainException()
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
    public void Create_WhenBreakExactlyAtShiftBoundaries_Succeeds()
    {
        var schedule = WorkSchedule.Create(
            EmployeeGroupId,
            new TimeOnly(8, 0),
            new TimeOnly(16, 0),
            new TimeOnly(8, 0),
            new TimeOnly(16, 0),
            0, 0);

        Assert.Equal(new TimeOnly(8, 0), schedule.BreakStartTime);
        Assert.Equal(new TimeOnly(16, 0), schedule.BreakEndTime);
    }

    [Fact]
    public void Create_WhenBreakInsideShift_Succeeds()
    {
        var schedule = WorkSchedule.Create(
            EmployeeGroupId,
            new TimeOnly(8, 0),
            new TimeOnly(16, 0),
            new TimeOnly(10, 0),
            new TimeOnly(11, 0),
            0, 0);

        Assert.Equal(new TimeOnly(10, 0), schedule.BreakStartTime);
        Assert.Equal(new TimeOnly(11, 0), schedule.BreakEndTime);
    }

    #endregion

    #region Create - Lateness/Earliness Validation

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

    [Fact]
    public void Create_WhenCheckInLatenessZero_Succeeds()
    {
        var schedule = WorkSchedule.Create(
            EmployeeGroupId,
            new TimeOnly(8, 0),
            new TimeOnly(16, 0),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            0, 0);

        Assert.Equal(0, schedule.AllowedCheckInLatenessMinutes);
    }

    [Fact]
    public void Create_WhenCheckOutEarlinessZero_Succeeds()
    {
        var schedule = WorkSchedule.Create(
            EmployeeGroupId,
            new TimeOnly(8, 0),
            new TimeOnly(16, 0),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            0, 0);

        Assert.Equal(0, schedule.AllowedCheckOutEarlinessMinutes);
    }

    [Fact]
    public void Create_WhenBothCheckInAndCheckOutNegative_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            WorkSchedule.Create(
                EmployeeGroupId,
                new TimeOnly(8, 0),
                new TimeOnly(16, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                -1, -1));

        Assert.Equal(WorkScheduleErrors.InvalidCheckInLateness.Code, exception.Error.Code);
    }

    #endregion

    #region Activate

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var schedule = CreateSchedule();

        schedule.Activate();

        Assert.True(schedule.IsActive);
    }

    [Fact]
    public void Activate_RaisesWorkScheduleActivatedDomainEvent()
    {
        var schedule = CreateSchedule();

        schedule.Activate();

        var domainEvent = schedule.DomainEvents
            .OfType<WorkSheduleActivatedDomainEvent>()
            .SingleOrDefault();
        Assert.NotNull(domainEvent);
        Assert.Equal(schedule.Id, domainEvent.WorkScheduleId);
        Assert.Equal(EmployeeGroupId, domainEvent.EmployeeGroupId);
        Assert.True(domainEvent.ActivatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_RemainsActive()
    {
        var schedule = CreateSchedule();
        schedule.Activate();

        schedule.Activate();

        Assert.True(schedule.IsActive);
    }

    #endregion

    #region Deactivate

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var schedule = CreateSchedule();
        schedule.Activate();

        schedule.Deactivate();

        Assert.False(schedule.IsActive);
    }

    [Fact]
    public void Deactivate_RaisesWorkScheduleDeactivatedDomainEvent()
    {
        var schedule = CreateSchedule();
        schedule.Activate();
        schedule.ClearDomainEvent();

        schedule.Deactivate();

        var domainEvent = schedule.DomainEvents
            .OfType<WorkSheduleDeactivatedDomainEvent>()
            .SingleOrDefault();
        Assert.NotNull(domainEvent);
        Assert.Equal(schedule.Id, domainEvent.WorkScheduleId);
        Assert.Equal(EmployeeGroupId, domainEvent.EmployeeGroupId);
        Assert.True(domainEvent.DeactivatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_RemainsInactive()
    {
        var schedule = CreateSchedule();

        schedule.Deactivate();

        Assert.False(schedule.IsActive);
    }

    [Fact]
    public void Activate_ThenDeactivate_TogglesCorrectly()
    {
        var schedule = CreateSchedule();

        schedule.Activate();
        Assert.True(schedule.IsActive);

        schedule.Deactivate();
        Assert.False(schedule.IsActive);
    }

    #endregion

    #region Update

    [Fact]
    public void Update_UpdatesAllProperties()
    {
        var schedule = CreateSchedule();

        schedule.Update(
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            10, 10, 0);

        Assert.Equal(new TimeOnly(9, 0), schedule.ShiftStartTime);
        Assert.Equal(new TimeOnly(17, 0), schedule.ShiftEndTime);
        Assert.Equal(new TimeOnly(12, 0), schedule.BreakStartTime);
        Assert.Equal(new TimeOnly(13, 0), schedule.BreakEndTime);
        Assert.Equal(10, schedule.AllowedCheckInLatenessMinutes);
        Assert.Equal(10, schedule.AllowedCheckOutEarlinessMinutes);
        Assert.Equal(0, schedule.EndDayOffset);
    }

    [Fact]
    public void Update_NegativeLateness_ThrowsDomainException()
    {
        var schedule = CreateSchedule();

        var exception = Assert.Throws<DomainException>(() =>
            schedule.Update(
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                -1, 0, 0));

        Assert.Equal(WorkScheduleErrors.InvalidCheckInLateness.Code, exception.Error.Code);
    }

    [Fact]
    public void Update_NegativeEarliness_ThrowsDomainException()
    {
        var schedule = CreateSchedule();

        var exception = Assert.Throws<DomainException>(() =>
            schedule.Update(
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                0, -1, 0));

        Assert.Equal(WorkScheduleErrors.InvalidCheckOutEarliness.Code, exception.Error.Code);
    }

    [Fact]
    public void Update_NegativeEndDayOffset_ThrowsDomainException()
    {
        var schedule = CreateSchedule();

        var exception = Assert.Throws<DomainException>(() =>
            schedule.Update(
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                0, 0, -1));

        Assert.Equal(WorkScheduleErrors.InvalidEndDayOffset.Code, exception.Error.Code);
    }

    [Fact]
    public void Update_EndDayOffsetZero_Succeeds()
    {
        var schedule = CreateSchedule();

        schedule.Update(
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            0, 0, 0);

        Assert.Equal(0, schedule.EndDayOffset);
    }

    [Fact]
    public void Update_EndDayOffsetPositive_Succeeds()
    {
        var schedule = CreateSchedule();

        schedule.Update(
            new TimeOnly(22, 0),
            new TimeOnly(6, 0),
            new TimeOnly(23, 0),
            new TimeOnly(0, 0),
            0, 0, 1);

        Assert.Equal(1, schedule.EndDayOffset);
    }

    #endregion

    #region WorkTime

    [Fact]
    public void WorkTime_CalculatesCorrectlyForSameDayShift()
    {
        var schedule = CreateSchedule(
            shiftStart: new TimeOnly(8, 0),
            shiftEnd: new TimeOnly(16, 0),
            breakStart: new TimeOnly(12, 0),
            breakEnd: new TimeOnly(13, 0));

        Assert.Equal(TimeSpan.FromHours(8), schedule.WorkTime);
    }

    [Fact]
    public void WorkTime_CalculatesCorrectlyForCrossMidnightShift()
    {
        var schedule = WorkSchedule.Create(
            EmployeeGroupId,
            new TimeOnly(22, 0),
            new TimeOnly(6, 0),
            new TimeOnly(23, 0),
            new TimeOnly(0, 0),
            0, 0,
            endDayOffset: 1);

        Assert.Equal(TimeSpan.FromHours(8), schedule.WorkTime);
    }

    [Fact]
    public void WorkTime_CalculatesCorrectlyForShortShift()
    {
        var schedule = CreateSchedule(
            shiftStart: new TimeOnly(9, 0),
            shiftEnd: new TimeOnly(12, 0),
            breakStart: new TimeOnly(10, 0),
            breakEnd: new TimeOnly(10, 30));

        Assert.Equal(TimeSpan.FromHours(3), schedule.WorkTime);
    }

    #endregion
}
