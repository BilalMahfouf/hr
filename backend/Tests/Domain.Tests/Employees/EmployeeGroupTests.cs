using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.Domain.Common;

namespace Domain.Tests.Employees;

public sealed class EmployeeGroupTests
{
    private static EmployeeGroup CreateGroup(string name = "Day Shift", bool isSecurity = false, string? description = null) =>
        EmployeeGroup.Create(name, isSecurity, description);

    private static CreateWorkScheduleDto CreateScheduleDto(EmployeeGroupId groupId) =>
        new(
            groupId,
            ShiftStartTime: new TimeOnly(8, 0),
            ShiftEndTime: new TimeOnly(16, 0),
            EndDayOffset: 0,
            BreakStartTime: new TimeOnly(12, 0),
            BreakEndTime: new TimeOnly(13, 0),
            AllowedCheckInLatenessMinutes: 5,
            AllowedCheckOutEarlinessMinutes: 5);

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
        var dto = CreateScheduleDto(group.Id);

        group.AddWorkSchedule(dto);

        Assert.Single(group.WorkSchedules);
    }

    [Fact]
    public void AddWorkSchedule_SetsCorrectEmployeeGroupId()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);

        group.AddWorkSchedule(dto);

        Assert.Equal(group.Id, group.WorkSchedules.Single().EmployeeGroupId);
    }

    [Fact]
    public void AddWorkSchedule_SetsShiftTimes()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);

        group.AddWorkSchedule(dto);

        var schedule = group.WorkSchedules.Single();
        Assert.Equal(new TimeOnly(8, 0), schedule.ShiftStartTime);
        Assert.Equal(new TimeOnly(16, 0), schedule.ShiftEndTime);
    }

    [Fact]
    public void AddWorkSchedule_SetsBreakTimes()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);

        group.AddWorkSchedule(dto);

        var schedule = group.WorkSchedules.Single();
        Assert.Equal(new TimeOnly(12, 0), schedule.BreakStartTime);
        Assert.Equal(new TimeOnly(13, 0), schedule.BreakEndTime);
    }

    [Fact]
    public void AddWorkSchedule_SetsLatenessAndEarliness()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);

        group.AddWorkSchedule(dto);

        var schedule = group.WorkSchedules.Single();
        Assert.Equal(5, schedule.AllowedCheckInLatenessMinutes);
        Assert.Equal(5, schedule.AllowedCheckOutEarlinessMinutes);
    }

    [Fact]
    public void AddWorkSchedule_SetsEndDayOffset()
    {
        var group = CreateGroup();
        var dto = new CreateWorkScheduleDto(
            group.Id,
            ShiftStartTime: new TimeOnly(22, 0),
            ShiftEndTime: new TimeOnly(6, 0),
            EndDayOffset: 1,
            BreakStartTime: new TimeOnly(23, 0),
            BreakEndTime: new TimeOnly(0, 0),
            AllowedCheckInLatenessMinutes: 0,
            AllowedCheckOutEarlinessMinutes: 0);

        group.AddWorkSchedule(dto);

        var schedule = group.WorkSchedules.Single();
        Assert.Equal(1, schedule.EndDayOffset);
    }

    [Fact]
    public void AddWorkSchedule_NewScheduleIsNotActive()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);

        group.AddWorkSchedule(dto);

        Assert.False(group.WorkSchedules.Single().IsActive);
    }

    [Fact]
    public void AddWorkSchedule_MultipleSchedules_AllAdded()
    {
        var group = CreateGroup();
        var dto1 = CreateScheduleDto(group.Id);
        var dto2 = new CreateWorkScheduleDto(
            group.Id,
            ShiftStartTime: new TimeOnly(20, 0),
            ShiftEndTime: new TimeOnly(4, 0),
            EndDayOffset: 1,
            BreakStartTime: new TimeOnly(23, 0),
            BreakEndTime: new TimeOnly(0, 0),
            AllowedCheckInLatenessMinutes: 0,
            AllowedCheckOutEarlinessMinutes: 0);

        group.AddWorkSchedule(dto1);
        group.AddWorkSchedule(dto2);

        Assert.Equal(2, group.WorkSchedules.Count);
    }

    [Fact]
    public void AddWorkSchedule_WhenBelongsToAnotherGroup_ThrowsDomainException()
    {
        var group = CreateGroup();
        var otherGroup = CreateGroup(name: "Other");
        var dto = CreateScheduleDto(otherGroup.Id);

        var exception = Assert.Throws<DomainException>(() => group.AddWorkSchedule(dto));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup.Code, exception.Error.Code);
        Assert.Empty(group.WorkSchedules);
    }

    #endregion

    #region RemoveWorkSchedule

    [Fact]
    public void RemoveWorkSchedule_RemovesFromCollection()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var schedule = group.WorkSchedules.Single();

        group.RemoveWorkSchedule(schedule);

        Assert.Empty(group.WorkSchedules);
    }

    [Fact]
    public void RemoveWorkSchedule_WhenBelongsToAnotherGroup_ThrowsDomainException()
    {
        var group = CreateGroup();
        var otherGroup = CreateGroup(name: "Other");
        var otherDto = CreateScheduleDto(otherGroup.Id);
        otherGroup.AddWorkSchedule(otherDto);
        var scheduleFromOther = otherGroup.WorkSchedules.Single();

        var exception = Assert.Throws<DomainException>(() => group.RemoveWorkSchedule(scheduleFromOther));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup.Code, exception.Error.Code);
        Assert.Empty(group.WorkSchedules);
    }

    [Fact]
    public void RemoveWorkSchedule_FromMultiple_RemovesOnlyTarget()
    {
        var group = CreateGroup();
        var dto1 = CreateScheduleDto(group.Id);
        var dto2 = new CreateWorkScheduleDto(
            group.Id,
            ShiftStartTime: new TimeOnly(20, 0),
            ShiftEndTime: new TimeOnly(4, 0),
            EndDayOffset: 1,
            BreakStartTime: new TimeOnly(23, 0),
            BreakEndTime: new TimeOnly(0, 0),
            AllowedCheckInLatenessMinutes: 0,
            AllowedCheckOutEarlinessMinutes: 0);
        group.AddWorkSchedule(dto1);
        group.AddWorkSchedule(dto2);
        var first = group.WorkSchedules.First();

        group.RemoveWorkSchedule(first);

        Assert.Single(group.WorkSchedules);
    }

    #endregion

    #region UpdateWorkSchedule

    [Fact]
    public void UpdateWorkSchedule_ReplacesExistingSchedule()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var existing = group.WorkSchedules.Single();

        var updateDto = new UpdateWorkScheduleDto(
            existing.Id,
            group.Id,
            ShiftStartTime: new TimeOnly(9, 0),
            ShiftEndTime: new TimeOnly(17, 0),
            EndDayOffset: 0,
            BreakStartTime: new TimeOnly(12, 0),
            BreakEndTime: new TimeOnly(13, 0),
            AllowedCheckInLatenessMinutes: 10,
            AllowedCheckOutEarlinessMinutes: 10);

        group.UpdateWorkSchedule(updateDto);

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
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var existing = group.WorkSchedules.Single();

        var updateDto = new UpdateWorkScheduleDto(
            existing.Id,
            group.Id,
            ShiftStartTime: new TimeOnly(9, 0),
            ShiftEndTime: new TimeOnly(17, 0),
            EndDayOffset: 0,
            BreakStartTime: new TimeOnly(12, 0),
            BreakEndTime: new TimeOnly(13, 0),
            AllowedCheckInLatenessMinutes: 10,
            AllowedCheckOutEarlinessMinutes: 10);

        group.UpdateWorkSchedule(updateDto);

        Assert.Single(group.WorkSchedules);
    }

    [Fact]
    public void UpdateWorkSchedule_GeneratesNewScheduleId()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var existingId = group.WorkSchedules.Single().Id;

        var updateDto = new UpdateWorkScheduleDto(
            existingId,
            group.Id,
            ShiftStartTime: new TimeOnly(9, 0),
            ShiftEndTime: new TimeOnly(17, 0),
            EndDayOffset: 0,
            BreakStartTime: new TimeOnly(12, 0),
            BreakEndTime: new TimeOnly(13, 0),
            AllowedCheckInLatenessMinutes: 10,
            AllowedCheckOutEarlinessMinutes: 10);

        group.UpdateWorkSchedule(updateDto);

        var newId = group.WorkSchedules.Single().Id;
        Assert.NotEqual(existingId, newId);
    }

    #endregion

    #region ActivateWorkSchedule

    [Fact]
    public void ActivateWorkSchedule_SetsScheduleIsActiveTrue()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var schedule = group.WorkSchedules.Single();

        group.ActivateWorkSchedule(schedule);

        Assert.True(schedule.IsActive);
    }

    [Fact]
    public void ActivateWorkSchedule_RaisesActivatedDomainEvent()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
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
        var dto = CreateScheduleDto(otherGroup.Id);
        otherGroup.AddWorkSchedule(dto);
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
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var schedule = group.WorkSchedules.Single();
        group.ActivateWorkSchedule(schedule);

        group.DeactivateWorkSchedule(schedule);

        Assert.False(schedule.IsActive);
    }

    [Fact]
    public void DeactivateWorkSchedule_RaisesDeactivatedDomainEvent()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
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
        var dto = CreateScheduleDto(otherGroup.Id);
        otherGroup.AddWorkSchedule(dto);
        otherGroup.ActivateWorkSchedule(otherGroup.WorkSchedules.Single());
        var scheduleFromOther = otherGroup.WorkSchedules.Single();

        var exception = Assert.Throws<DomainException>(() => group.DeactivateWorkSchedule(scheduleFromOther));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleBelongsToAnotherGroup.Code, exception.Error.Code);
        Assert.True(scheduleFromOther.IsActive);
    }

    #endregion

    #region DoesTheGroupWork

    [Fact]
    public void DoesTheGroupWork_WithNoRotationEntries_ThrowsDivideByZeroException()
    {
        var group = CreateGroup();

        Assert.Throws<DivideByZeroException>(() => group.DoesTheGroupWork(DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    #endregion

    #region GetRotation

    [Fact]
    public void GetRotation_WhenDateBeforeStart_ThrowsDomainException()
    {
        var group = CreateGroup();

        var method = typeof(EmployeeGroup).GetMethod("GetRotation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        Assert.ThrowsAny<Exception>(() => method.Invoke(group, [pastDate]));
    }

    #endregion

    #region GetGroupWorkScheduleInDateTime

    [Fact]
    public void GetGroupWorkScheduleInDateTime_WithNoRotationEntries_ThrowsDivideByZeroException()
    {
        var group = CreateGroup();

        Assert.Throws<DivideByZeroException>(() =>
            group.GetGroupWorkScheduleInDateTime(DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    #endregion
}
