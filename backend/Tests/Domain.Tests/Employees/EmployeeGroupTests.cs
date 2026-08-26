using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.Domain.Common;

namespace Domain.Tests.Employees;

public sealed class EmployeeGroupTests
{
    private static EmployeeGroup CreateGroup(
        string name = "Day Shift",
        bool isSecurity = false,
        string? description = null) =>
        EmployeeGroup.Create(name, isSecurity, description);

    private static void AddDefaultSchedule(EmployeeGroup group) =>
        group.AddWorkSchedule(
            new TimeOnly(8, 0),
            new TimeOnly(16, 0),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            allowedCheckInLatenessMinutes: 5,
            allowedCheckOutEarlinessMinutes: 5,
            endDayOffset: 0);

    #region Create

    [Fact]
    public void Create_SetsExpectedInitialState()
    {
        var group = CreateGroup();

        Assert.NotEqual(Guid.Empty, group.Id.Value);
        Assert.Equal("Day Shift", group.Name);
        Assert.False(group.IsSecurity);
        Assert.Null(group.Description);
        Assert.Empty(group.WorkSchedules);
        Assert.Empty(group.RotationEntries);
        Assert.Equal(0, group.NumberOfRotations);
    }

    [Fact]
    public void Create_WithDescription_SetsDescription()
    {
        var group = CreateGroup(name: "Night Shift", isSecurity: true, description: "Security group");

        Assert.Equal("Night Shift", group.Name);
        Assert.True(group.IsSecurity);
        Assert.Equal("Security group", group.Description);
    }

    [Fact]
    public void Create_WithIsSecurityTrue_SetsIsSecurity()
    {
        var group = CreateGroup(isSecurity: true);

        Assert.True(group.IsSecurity);
    }

    [Fact]
    public void Create_WithIsSecurityFalse_SetsIsSecurityFalse()
    {
        var group = CreateGroup(isSecurity: false);

        Assert.False(group.IsSecurity);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var group1 = CreateGroup();
        var group2 = CreateGroup();

        Assert.NotEqual(group1.Id, group2.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNameNullOrWhitespace_ThrowsDomainException(string name)
    {
        var exception = Assert.Throws<DomainException>(() =>
            EmployeeGroup.Create(name, isSecurity: false));

        Assert.Equal(EmployeeGroupErrors.InvalidName.Code, exception.Error.Code);
    }

    [Fact]
    public void Create_WhenNameNull_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            EmployeeGroup.Create(null!, isSecurity: false));

        Assert.Equal(EmployeeGroupErrors.InvalidName.Code, exception.Error.Code);
    }

    #endregion

    #region AddWorkSchedule

    [Fact]
    public void AddWorkSchedule_AddsToCollection()
    {
        var group = CreateGroup();

        AddDefaultSchedule(group);

        Assert.Single(group.WorkSchedules);
    }

    [Fact]
    public void AddWorkSchedule_SetsCorrectEmployeeGroupId()
    {
        var group = CreateGroup();

        AddDefaultSchedule(group);

        Assert.Equal(group.Id, group.WorkSchedules.Single().EmployeeGroupId);
    }

    [Fact]
    public void AddWorkSchedule_SetsShiftTimes()
    {
        var group = CreateGroup();

        AddDefaultSchedule(group);

        var schedule = group.WorkSchedules.Single();
        Assert.Equal(new TimeOnly(8, 0), schedule.ShiftStartTime);
        Assert.Equal(new TimeOnly(16, 0), schedule.ShiftEndTime);
    }

    [Fact]
    public void AddWorkSchedule_SetsBreakTimes()
    {
        var group = CreateGroup();

        AddDefaultSchedule(group);

        var schedule = group.WorkSchedules.Single();
        Assert.Equal(new TimeOnly(12, 0), schedule.BreakStartTime);
        Assert.Equal(new TimeOnly(13, 0), schedule.BreakEndTime);
    }

    [Fact]
    public void AddWorkSchedule_SetsLatenessAndEarliness()
    {
        var group = CreateGroup();

        AddDefaultSchedule(group);

        var schedule = group.WorkSchedules.Single();
        Assert.Equal(5, schedule.AllowedCheckInLatenessMinutes);
        Assert.Equal(5, schedule.AllowedCheckOutEarlinessMinutes);
    }

    [Fact]
    public void AddWorkSchedule_SetsEndDayOffset()
    {
        var group = CreateGroup();

        group.AddWorkSchedule(
            new TimeOnly(22, 0),
            new TimeOnly(6, 0),
            new TimeOnly(23, 0),
            new TimeOnly(0, 0),
            0, 0,
            endDayOffset: 1);

        var schedule = group.WorkSchedules.Single();
        Assert.Equal(1, schedule.EndDayOffset);
    }

    [Fact]
    public void AddWorkSchedule_NewScheduleIsNotActive()
    {
        var group = CreateGroup();

        AddDefaultSchedule(group);

        Assert.False(group.WorkSchedules.Single().IsActive);
    }

    [Fact]
    public void AddWorkSchedule_MultipleSchedules_AllAdded()
    {
        var group = CreateGroup();

        AddDefaultSchedule(group);
        group.AddWorkSchedule(
            new TimeOnly(20, 0),
            new TimeOnly(4, 0),
            new TimeOnly(23, 0),
            new TimeOnly(0, 0),
            0, 0,
            endDayOffset: 1);

        Assert.Equal(2, group.WorkSchedules.Count);
    }

    [Fact]
    public void AddWorkSchedule_InvalidShiftRange_ThrowsDomainException()
    {
        var group = CreateGroup();

        var exception = Assert.Throws<DomainException>(() =>
            group.AddWorkSchedule(
                new TimeOnly(16, 0),
                new TimeOnly(8, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                0, 0, 0));

        Assert.Equal(WorkScheduleErrors.InvalidShiftRange.Code, exception.Error.Code);
    }

    #endregion

    #region UpdateWorkSchedule

    [Fact]
    public void UpdateWorkSchedule_UpdatesExistingSchedule()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var existingId = group.WorkSchedules.Single().Id;

        group.UpdateWorkSchedule(
            existingId,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            10, 10, 0);

        var updated = group.WorkSchedules.Single();
        Assert.Equal(new TimeOnly(9, 0), updated.ShiftStartTime);
        Assert.Equal(new TimeOnly(17, 0), updated.ShiftEndTime);
        Assert.Equal(10, updated.AllowedCheckInLatenessMinutes);
        Assert.Equal(10, updated.AllowedCheckOutEarlinessMinutes);
    }

    [Fact]
    public void UpdateWorkSchedule_CollectionCountRemainsSame()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var existingId = group.WorkSchedules.Single().Id;

        group.UpdateWorkSchedule(
            existingId,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            10, 10, 0);

        Assert.Single(group.WorkSchedules);
    }

    [Fact]
    public void UpdateWorkSchedule_SameId_KeepsSameId()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var existingId = group.WorkSchedules.Single().Id;

        group.UpdateWorkSchedule(
            existingId,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            10, 10, 0);

        Assert.Equal(existingId, group.WorkSchedules.Single().Id);
    }

    [Fact]
    public void UpdateWorkSchedule_NotFound_ThrowsDomainException()
    {
        var group = CreateGroup();
        var fakeId = WorkScheduleId.New();

        var exception = Assert.Throws<DomainException>(() =>
            group.UpdateWorkSchedule(
                fakeId,
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                10, 10, 0));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup.Code, exception.Error.Code);
    }

    [Fact]
    public void UpdateWorkSchedule_NegativeLateness_ThrowsDomainException()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var existingId = group.WorkSchedules.Single().Id;

        var exception = Assert.Throws<DomainException>(() =>
            group.UpdateWorkSchedule(
                existingId,
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                -1, 0, 0));

        Assert.Equal(WorkScheduleErrors.InvalidCheckInLateness.Code, exception.Error.Code);
    }

    [Fact]
    public void UpdateWorkSchedule_NegativeEarliness_ThrowsDomainException()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var existingId = group.WorkSchedules.Single().Id;

        var exception = Assert.Throws<DomainException>(() =>
            group.UpdateWorkSchedule(
                existingId,
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                0, -1, 0));

        Assert.Equal(WorkScheduleErrors.InvalidCheckOutEarliness.Code, exception.Error.Code);
    }

    [Fact]
    public void UpdateWorkSchedule_NegativeEndDayOffset_ThrowsDomainException()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var existingId = group.WorkSchedules.Single().Id;

        var exception = Assert.Throws<DomainException>(() =>
            group.UpdateWorkSchedule(
                existingId,
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                new TimeOnly(12, 0),
                new TimeOnly(13, 0),
                0, 0, -1));

        Assert.Equal(WorkScheduleErrors.InvalidEndDayOffset.Code, exception.Error.Code);
    }

    #endregion

    #region RemoveWorkSchedule

    [Fact]
    public void RemoveWorkSchedule_RemovesFromCollection()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;

        group.RemoveWorkSchedule(scheduleId);

        Assert.Empty(group.WorkSchedules);
    }

    [Fact]
    public void RemoveWorkSchedule_NotFound_ThrowsDomainException()
    {
        var group = CreateGroup();
        var fakeId = WorkScheduleId.New();

        var exception = Assert.Throws<DomainException>(() =>
            group.RemoveWorkSchedule(fakeId));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup.Code, exception.Error.Code);
    }

    [Fact]
    public void RemoveWorkSchedule_UsedByRotation_ThrowsDomainException()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;
        group.AddWorkRotation(1, scheduleId);

        var exception = Assert.Throws<DomainException>(() =>
            group.RemoveWorkSchedule(scheduleId));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleUsedByRotation.Code, exception.Error.Code);
        Assert.Single(group.WorkSchedules);
    }

    [Fact]
    public void RemoveWorkSchedule_FromMultiple_RemovesOnlyTarget()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        group.AddWorkSchedule(
            new TimeOnly(20, 0),
            new TimeOnly(4, 0),
            new TimeOnly(23, 0),
            new TimeOnly(0, 0),
            0, 0,
            endDayOffset: 1);
        var firstId = group.WorkSchedules.First().Id;

        group.RemoveWorkSchedule(firstId);

        Assert.Single(group.WorkSchedules);
    }

    #endregion

    #region ActivateWorkSchedule

    [Fact]
    public void ActivateWorkSchedule_SetsScheduleIsActiveTrue()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var schedule = group.WorkSchedules.Single();

        group.ActivateWorkSchedule(schedule);

        Assert.True(schedule.IsActive);
    }

    [Fact]
    public void ActivateWorkSchedule_RaisesActivatedDomainEvent()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var schedule = group.WorkSchedules.Single();

        group.ActivateWorkSchedule(schedule);

        var domainEvent = schedule.DomainEvents
            .OfType<WorkSheduleActivatedDomainEvent>()
            .SingleOrDefault();
        Assert.NotNull(domainEvent);
        Assert.Equal(schedule.Id, domainEvent.WorkScheduleId);
        Assert.Equal(group.Id, domainEvent.EmployeeGroupId);
    }

    [Fact]
    public void ActivateWorkSchedule_WhenBelongsToAnotherGroup_ThrowsDomainException()
    {
        var group = CreateGroup();
        var otherGroup = CreateGroup(name: "Other");
        otherGroup.AddWorkSchedule(
            new TimeOnly(8, 0), new TimeOnly(16, 0),
            new TimeOnly(12, 0), new TimeOnly(13, 0),
            0, 0, 0);
        var scheduleFromOther = otherGroup.WorkSchedules.Single();

        var exception = Assert.Throws<DomainException>(() => group.ActivateWorkSchedule(scheduleFromOther));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup.Code, exception.Error.Code);
        Assert.False(scheduleFromOther.IsActive);
    }

    #endregion

    #region DeactivateWorkSchedule

    [Fact]
    public void DeactivateWorkSchedule_SetsScheduleIsActiveFalse()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var schedule = group.WorkSchedules.Single();
        group.ActivateWorkSchedule(schedule);

        group.DeactivateWorkSchedule(schedule);

        Assert.False(schedule.IsActive);
    }

    [Fact]
    public void DeactivateWorkSchedule_RaisesDeactivatedDomainEvent()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var schedule = group.WorkSchedules.Single();
        group.ActivateWorkSchedule(schedule);
        schedule.ClearDomainEvent();

        group.DeactivateWorkSchedule(schedule);

        var domainEvent = schedule.DomainEvents
            .OfType<WorkSheduleDeactivatedDomainEvent>()
            .SingleOrDefault();
        Assert.NotNull(domainEvent);
        Assert.Equal(schedule.Id, domainEvent.WorkScheduleId);
        Assert.Equal(group.Id, domainEvent.EmployeeGroupId);
    }

    [Fact]
    public void DeactivateWorkSchedule_WhenBelongsToAnotherGroup_ThrowsDomainException()
    {
        var group = CreateGroup();
        var otherGroup = CreateGroup(name: "Other");
        otherGroup.AddWorkSchedule(
            new TimeOnly(8, 0), new TimeOnly(16, 0),
            new TimeOnly(12, 0), new TimeOnly(13, 0),
            0, 0, 0);
        otherGroup.ActivateWorkSchedule(otherGroup.WorkSchedules.Single());
        var scheduleFromOther = otherGroup.WorkSchedules.Single();

        var exception = Assert.Throws<DomainException>(() => group.DeactivateWorkSchedule(scheduleFromOther));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup.Code, exception.Error.Code);
        Assert.True(scheduleFromOther.IsActive);
    }

    #endregion

    #region AddWorkRotation / AddRestRotation

    [Fact]
    public void AddWorkRotation_AddsToRotationEntries()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;

        group.AddWorkRotation(1, scheduleId);

        Assert.Single(group.RotationEntries);
        Assert.Equal(1, group.RotationEntries.Single().Position);
        Assert.Equal(RotationStatus.Work, group.RotationEntries.Single().Status);
    }

    [Fact]
    public void AddWorkRotation_IncrementsNumberOfRotations()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;

        group.AddWorkRotation(1, scheduleId);

        Assert.Equal(1, group.NumberOfRotations);
    }

    [Fact]
    public void AddRestRotation_AddsToRotationEntries()
    {
        var group = CreateGroup();

        group.AddRestRotation(1);

        Assert.Single(group.RotationEntries);
        Assert.Equal(1, group.RotationEntries.Single().Position);
        Assert.Equal(RotationStatus.Rest, group.RotationEntries.Single().Status);
    }

    [Fact]
    public void AddWorkRotation_MultiplePositions_AllAdded()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;

        group.AddWorkRotation(1, scheduleId);
        group.AddRestRotation(2);
        group.AddWorkRotation(3, scheduleId);

        Assert.Equal(3, group.NumberOfRotations);
    }

    [Fact]
    public void AddWorkRotation_PositionLessThan1_ThrowsDomainException()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;

        var exception = Assert.Throws<DomainException>(() =>
            group.AddWorkRotation(0, scheduleId));

        Assert.Equal(RotationEntryErrors.InvalidPosition.Code, exception.Error.Code);
    }

    [Fact]
    public void AddWorkRotation_DuplicatePosition_ThrowsDomainException()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;
        group.AddWorkRotation(1, scheduleId);

        var exception = Assert.Throws<DomainException>(() =>
            group.AddWorkRotation(1, scheduleId));

        Assert.Equal(EmployeeGroupErrors.RotationPositionAlreadyExists.Code, exception.Error.Code);
        Assert.Single(group.RotationEntries);
    }

    #endregion

    #region UpdateRotation

    [Fact]
    public void UpdateRotation_ChangesRotationEntry()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;
        group.AddWorkRotation(1, scheduleId);

        group.UpdateRotation(1, null);

        Assert.Equal(RotationStatus.Rest, group.RotationEntries.Single().Status);
    }

    [Fact]
    public void UpdateRotation_NotFound_ThrowsDomainException()
    {
        var group = CreateGroup();

        var exception = Assert.Throws<DomainException>(() =>
            group.UpdateRotation(99, null));

        Assert.Equal(EmployeeGroupErrors.RotationNotFound.Code, exception.Error.Code);
    }

    #endregion

    #region RemoveRotation

    [Fact]
    public void RemoveRotation_RemovesFromCollection()
    {
        var group = CreateGroup();
        group.AddRestRotation(1);
        group.AddRestRotation(2);

        group.RemoveRotation(1);

        Assert.Single(group.RotationEntries);
        Assert.Equal(2, group.RotationEntries.Single().Position);
    }

    [Fact]
    public void RemoveRotation_NotFound_ThrowsDomainException()
    {
        var group = CreateGroup();

        var exception = Assert.Throws<DomainException>(() =>
            group.RemoveRotation(99));

        Assert.Equal(EmployeeGroupErrors.RotationNotFound.Code, exception.Error.Code);
    }

    #endregion

    #region DoesTheGroupWork

    private static DateOnly GetDeterministicDateForPosition(int position, int totalRotations)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysElapsed = today.DayNumber - DateOnly.MinValue.DayNumber;
        var currentPosition = (daysElapsed % totalRotations) + 1;
        if (currentPosition == position)
            return today;
        var offset = position - currentPosition;
        if (offset < 0) offset += totalRotations;
        return today.AddDays(offset);
    }

    [Fact]
    public void DoesTheGroupWork_WorkDay_ReturnsTrue()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;
        group.AddWorkRotation(1, scheduleId);
        group.AddRestRotation(2);
        var workDate = GetDeterministicDateForPosition(1, 2);

        var result = group.DoesTheGroupWork(workDate);

        Assert.True(result);
    }

    [Fact]
    public void DoesTheGroupWork_RestDay_ReturnsFalse()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;
        group.AddWorkRotation(1, scheduleId);
        group.AddRestRotation(2);
        var restDate = GetDeterministicDateForPosition(2, 2);

        var result = group.DoesTheGroupWork(restDate);

        Assert.False(result);
    }

    [Fact]
    public void DoesTheGroupWork_NoRotationEntries_ThrowsDivideByZeroException()
    {
        var group = CreateGroup();

        Assert.Throws<DivideByZeroException>(() =>
            group.DoesTheGroupWork(DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public void DoesTheGroupWork_ThreeRotations_CyclesCorrectly()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;
        group.AddWorkRotation(1, scheduleId);
        group.AddRestRotation(2);
        group.AddWorkRotation(3, scheduleId);

        var date1 = GetDeterministicDateForPosition(1, 3);
        var date2 = GetDeterministicDateForPosition(2, 3);
        var date3 = GetDeterministicDateForPosition(3, 3);

        Assert.True(group.DoesTheGroupWork(date1));
        Assert.False(group.DoesTheGroupWork(date2));
        Assert.True(group.DoesTheGroupWork(date3));
    }

    #endregion

    #region GetGroupWorkScheduleInDateTime

    [Fact]
    public void GetGroupWorkScheduleInDateTime_WorkDay_ReturnsSchedule()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;
        group.AddWorkRotation(1, scheduleId);
        group.AddRestRotation(2);
        var workDate = GetDeterministicDateForPosition(1, 2);

        var result = group.GetGroupWorkScheduleInDateTime(workDate);

        Assert.NotNull(result);
        Assert.Equal(workDate.ToDateTime(new TimeOnly(8, 0)), result.ExpectedCheckInAt);
        Assert.Equal(workDate.ToDateTime(new TimeOnly(16, 0)), result.ExpectedCheckoutAt);
    }

    [Fact]
    public void GetGroupWorkScheduleInDateTime_RestDay_WithPrevScheduleOffset_ReturnsCarryOver()
    {
        var group = CreateGroup();
        group.AddWorkSchedule(
            new TimeOnly(22, 0),
            new TimeOnly(6, 0),
            new TimeOnly(23, 0),
            new TimeOnly(0, 0),
            0, 0,
            endDayOffset: 1);
        var scheduleId = group.WorkSchedules.Single().Id;
        group.AddWorkRotation(1, scheduleId);
        group.AddRestRotation(2);
        var restDate = GetDeterministicDateForPosition(2, 2);

        var result = group.GetGroupWorkScheduleInDateTime(restDate);

        Assert.NotNull(result);
        Assert.Equal(restDate.AddDays(-1).ToDateTime(new TimeOnly(22, 0)), result.ExpectedCheckInAt);
        Assert.Equal(restDate.ToDateTime(new TimeOnly(6, 0)), result.ExpectedCheckoutAt);
    }

    [Fact]
    public void GetGroupWorkScheduleInDateTime_RestDay_NoPrevScheduleOffset_ReturnsNull()
    {
        var group = CreateGroup();
        AddDefaultSchedule(group);
        var scheduleId = group.WorkSchedules.Single().Id;
        group.AddRestRotation(1);
        group.AddWorkRotation(2, scheduleId);
        var date = GetDeterministicDateForPosition(1, 2);

        var result = group.GetGroupWorkScheduleInDateTime(date);

        Assert.Null(result);
    }

    [Fact]
    public void GetGroupWorkScheduleInDateTime_NoRotationEntries_ThrowsDivideByZeroException()
    {
        var group = CreateGroup();

        Assert.Throws<DivideByZeroException>(() =>
            group.GetGroupWorkScheduleInDateTime(DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public void GetGroupWorkScheduleInDateTime_CrossMidnight_Shift_ReturnsCorrectTimes()
    {
        var group = CreateGroup();
        group.AddWorkSchedule(
            new TimeOnly(22, 0),
            new TimeOnly(6, 0),
            new TimeOnly(23, 0),
            new TimeOnly(0, 0),
            0, 0,
            endDayOffset: 1);
        var scheduleId = group.WorkSchedules.Single().Id;
        group.AddWorkRotation(1, scheduleId);
        group.AddRestRotation(2);
        var workDate = GetDeterministicDateForPosition(1, 2);

        var result = group.GetGroupWorkScheduleInDateTime(workDate);

        Assert.NotNull(result);
        Assert.Equal(workDate.ToDateTime(new TimeOnly(22, 0)), result.ExpectedCheckInAt);
        Assert.Equal(workDate.AddDays(1).ToDateTime(new TimeOnly(6, 0)), result.ExpectedCheckoutAt);
    }

    #endregion
}
