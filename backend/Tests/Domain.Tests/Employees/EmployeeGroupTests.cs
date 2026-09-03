using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.Domain.Common;

namespace Domain.Tests.Employees;

public sealed class EmployeeGroupTests
{
    private static EmployeeGroup CreateGroup(
        string employeeGroupNumber = "GRP-001",
        string name = "Day Shift",
        bool isSecurity = false,
        string? description = null,
        DateOnly? rotationStartDate = null) =>
        EmployeeGroup.Create(employeeGroupNumber, name, isSecurity, rotationStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow), description);

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
            EmployeeGroup.Create("GRP-001", name, isSecurity: false, rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(EmployeeGroupErrors.InvalidName.Code, exception.Error.Code);
    }

    [Fact]
    public void Create_WhenNameNull_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            EmployeeGroup.Create("GRP-001", null!, isSecurity: false, rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(EmployeeGroupErrors.InvalidName.Code, exception.Error.Code);
    }

    [Fact]
    public void Create_WhenRotationStartDateDefault_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            EmployeeGroup.Create("GRP-001", "Test", isSecurity: false, rotationStartDate: default));

        Assert.Equal(EmployeeGroupErrors.RotationStartDateRequired.Code, exception.Error.Code);
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

        group.RemoveWorkSchedule(schedule.Id);

        Assert.Empty(group.WorkSchedules);
    }

    [Fact]
    public void RemoveWorkSchedule_WhenNotInGroup_ThrowsDomainException()
    {
        var group = CreateGroup();
        var otherGroup = CreateGroup(name: "Other");
        var otherDto = CreateScheduleDto(otherGroup.Id);
        otherGroup.AddWorkSchedule(otherDto);
        var scheduleFromOther = otherGroup.WorkSchedules.Single();

        var exception = Assert.Throws<DomainException>(() => group.RemoveWorkSchedule(scheduleFromOther.Id));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, exception.Error.Code);
        Assert.Empty(group.WorkSchedules);
    }

    [Fact]
    public void RemoveWorkSchedule_WhenReferencedByRotation_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var schedule = group.WorkSchedules.Single();

        group.AddRotationEntry(1, schedule.Id);

        var exception = Assert.Throws<DomainException>(() => group.RemoveWorkSchedule(schedule.Id));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleInUse.Code, exception.Error.Code);
        Assert.Single(group.WorkSchedules);
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

        group.RemoveWorkSchedule(first.Id);

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

        group.ActivateWorkSchedule(schedule.Id);

        Assert.True(schedule.IsActive);
    }

    [Fact]
    public void ActivateWorkSchedule_RaisesActivatedDomainEvent()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var schedule = group.WorkSchedules.Single();

        group.ActivateWorkSchedule(schedule.Id);

        var domainEvent = schedule.DomainEvents
            .OfType<WorkSheduleActivatedDomainEvent>()
            .SingleOrDefault();
        Assert.NotNull(domainEvent);
        Assert.Equal(schedule.Id, domainEvent.WorkScheduleId);
        Assert.Equal(group.Id, domainEvent.EmployeeGroupId);
    }

    [Fact]
    public void ActivateWorkSchedule_WhenNotInGroup_ThrowsDomainException()
    {
        var group = CreateGroup();
        var otherGroup = CreateGroup(name: "Other");
        var dto = CreateScheduleDto(otherGroup.Id);
        otherGroup.AddWorkSchedule(dto);
        var scheduleFromOther = otherGroup.WorkSchedules.Single();

        var exception = Assert.Throws<DomainException>(() => group.ActivateWorkSchedule(scheduleFromOther.Id));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, exception.Error.Code);
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
        group.ActivateWorkSchedule(schedule.Id);

        group.DeactivateWorkSchedule(schedule.Id);

        Assert.False(schedule.IsActive);
    }

    [Fact]
    public void DeactivateWorkSchedule_RaisesDeactivatedDomainEvent()
    {
        var group = CreateGroup();
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var schedule = group.WorkSchedules.Single();
        group.ActivateWorkSchedule(schedule.Id);

        schedule.ClearDomainEvent();
        group.DeactivateWorkSchedule(schedule.Id);

        var domainEvent = schedule.DomainEvents
            .OfType<WorkSheduleDeactivatedDomainEvent>()
            .SingleOrDefault();
        Assert.NotNull(domainEvent);
        Assert.Equal(schedule.Id, domainEvent.WorkScheduleId);
        Assert.Equal(group.Id, domainEvent.EmployeeGroupId);
    }

    [Fact]
    public void DeactivateWorkSchedule_WhenNotInGroup_ThrowsDomainException()
    {
        var group = CreateGroup();
        var otherGroup = CreateGroup(name: "Other");
        var dto = CreateScheduleDto(otherGroup.Id);
        otherGroup.AddWorkSchedule(dto);
        otherGroup.ActivateWorkSchedule(otherGroup.WorkSchedules.Single().Id);
        var scheduleFromOther = otherGroup.WorkSchedules.Single();

        var exception = Assert.Throws<DomainException>(() => group.DeactivateWorkSchedule(scheduleFromOther.Id));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, exception.Error.Code);
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
    public void GetRotation_WhenDateBeforeStart_ReturnsNull()
    {
        var group = CreateGroup();

        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        var rotation = group.GetRotation(pastDate);

        Assert.Null(rotation);
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

    #region Rotation Tests

    [Fact]
    public void AddRotationEntry_WorkSchedule_AddsWorkRotation()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var schedule = group.WorkSchedules.Single();

        group.AddRotationEntry(1, schedule.Id);

        Assert.Single(group.RotationEntries);
        var entry = group.RotationEntries.Single();
        Assert.Equal(1, entry.Position);
        Assert.Equal(schedule.Id, entry.WorkScheduleId);
        Assert.Equal(RotationStatus.Work, entry.Status);
    }

    [Fact]
    public void AddRotationEntry_NullWorkScheduleId_AddsRestRotation()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));

        group.AddRotationEntry(1, null);

        Assert.Single(group.RotationEntries);
        var entry = group.RotationEntries.Single();
        Assert.Equal(1, entry.Position);
        Assert.Null(entry.WorkScheduleId);
        Assert.Equal(RotationStatus.Rest, entry.Status);
    }

    [Fact]
    public void AddRotationEntry_DuplicatePosition_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var schedule = group.WorkSchedules.Single();

        group.AddRotationEntry(1, schedule.Id);

        var exception = Assert.Throws<DomainException>(() => group.AddRotationEntry(1, null));

        Assert.Equal(EmployeeGroupErrors.DuplicateRotationPosition.Code, exception.Error.Code);
    }

    [Fact]
    public void AddRotationEntry_InvalidPosition_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var exception = Assert.Throws<DomainException>(() => group.AddRotationEntry(0, null));

        Assert.Equal(RotationEntryErrors.InvalidPosition.Code, exception.Error.Code);
    }

    [Fact]
    public void AddRotationEntry_WorkScheduleNotInGroup_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var otherGroup = CreateGroup(name: "Other", rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var dto = CreateScheduleDto(otherGroup.Id);
        otherGroup.AddWorkSchedule(dto);
        var scheduleFromOther = otherGroup.WorkSchedules.Single();

        var exception = Assert.Throws<DomainException>(() => group.AddRotationEntry(1, scheduleFromOther.Id));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, exception.Error.Code);
    }

    [Fact]
    public void RemoveRotationEntry_RemovesEntry()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        group.AddRotationEntry(1, null);

        group.RemoveRotationEntry(1);

        Assert.Empty(group.RotationEntries);
    }

    [Fact]
    public void RemoveRotationEntry_NonExistent_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var exception = Assert.Throws<DomainException>(() => group.RemoveRotationEntry(1));

        Assert.Equal(EmployeeGroupErrors.RotationEntryNotFound.Code, exception.Error.Code);
    }

    [Fact]
    public void ReplaceRotationEntries_ReplacesAllEntries()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var schedule = group.WorkSchedules.Single();

        group.AddRotationEntry(1, schedule.Id);
        group.AddRotationEntry(2, null);

        var newEntries = new List<(int Position, WorkScheduleId? WorkScheduleId)>
        {
            (1, null),
            (2, schedule.Id),
            (3, null)
        };

        group.ReplaceRotationEntries(newEntries);

        Assert.Equal(3, group.RotationEntries.Count);
        Assert.Equal(RotationStatus.Rest, group.RotationEntries.First(e => e.Position == 1).Status);
        Assert.Equal(RotationStatus.Work, group.RotationEntries.First(e => e.Position == 2).Status);
        Assert.Equal(RotationStatus.Rest, group.RotationEntries.First(e => e.Position == 3).Status);
    }

    [Fact]
    public void ReplaceRotationEntries_EmptyList_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var exception = Assert.Throws<DomainException>(() => group.ReplaceRotationEntries([]));

        Assert.Equal(EmployeeGroupErrors.InvalidRotationCount.Code, exception.Error.Code);
    }

    [Fact]
    public void ReplaceRotationEntries_DuplicatePositions_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var exception = Assert.Throws<DomainException>(() =>
            group.ReplaceRotationEntries(new List<(int, WorkScheduleId?)> { (1, null), (1, null) }));

        Assert.Equal(EmployeeGroupErrors.DuplicateRotationPosition.Code, exception.Error.Code);
    }

    [Fact]
    public void ReplaceRotationEntries_ScheduleNotInGroup_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var otherGroup = CreateGroup(name: "Other", rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var dto = CreateScheduleDto(otherGroup.Id);
        otherGroup.AddWorkSchedule(dto);
        var scheduleFromOther = otherGroup.WorkSchedules.Single();

        var exception = Assert.Throws<DomainException>(() =>
            group.ReplaceRotationEntries(new List<(int, WorkScheduleId?)> { (1, scheduleFromOther.Id) }));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, exception.Error.Code);
    }

    #endregion

    #region ReplaceSchedulesAndRotations Tests

    [Fact]
    public void ReplaceSchedulesAndRotations_ReplacesSchedulesAndRotationsAtomically()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var oldDto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(oldDto);
        group.AddRotationEntry(1, group.WorkSchedules.Single().Id);
        var originalScheduleId = group.WorkSchedules.Single().Id;

        var newDto = new CreateWorkScheduleDto(
            group.Id,
            ShiftStartTime: new TimeOnly(9, 0),
            ShiftEndTime: new TimeOnly(17, 0),
            EndDayOffset: 0,
            BreakStartTime: new TimeOnly(12, 0),
            BreakEndTime: new TimeOnly(13, 0),
            AllowedCheckInLatenessMinutes: 5,
            AllowedCheckOutEarlinessMinutes: 5);
        group.ReplaceSchedulesAndRotations(
            new List<CreateWorkScheduleDto> { newDto },
            new List<(int Position, int? WorkScheduleIndex)> { (1, 0), (2, null) });

        Assert.Equal(2, group.NumberOfRotations);
        Assert.Single(group.WorkSchedules);
        Assert.Equal(new TimeOnly(9, 0), group.WorkSchedules.Single().ShiftStartTime);
        Assert.NotEqual(originalScheduleId, group.WorkSchedules.Single().Id);

        var work = group.RotationEntries.First(e => e.Position == 1);
        Assert.Equal(RotationStatus.Work, work.Status);
        Assert.Equal(group.WorkSchedules.Single().Id, work.WorkScheduleId);

        var rest = group.RotationEntries.First(e => e.Position == 2);
        Assert.Equal(RotationStatus.Rest, rest.Status);
        Assert.Null(rest.WorkScheduleId);
    }

    [Fact]
    public void ReplaceSchedulesAndRotations_EmptyRotations_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var exception = Assert.Throws<DomainException>(() =>
            group.ReplaceSchedulesAndRotations(
                new List<CreateWorkScheduleDto> { CreateScheduleDto(group.Id) },
                new List<(int Position, int? WorkScheduleIndex)>()));

        Assert.Equal(EmployeeGroupErrors.InvalidRotationCount.Code, exception.Error.Code);
    }

    [Fact]
    public void ReplaceSchedulesAndRotations_InvalidScheduleIndex_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var exception = Assert.Throws<DomainException>(() =>
            group.ReplaceSchedulesAndRotations(
                new List<CreateWorkScheduleDto> { CreateScheduleDto(group.Id) },
                new List<(int Position, int? WorkScheduleIndex)> { (1, 5) }));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, exception.Error.Code);
    }

    [Fact]
    public void ReplaceSchedulesAndRotations_KeepsExistingStateWhenValidationFails()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var oldDto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(oldDto);
        group.AddRotationEntry(1, group.WorkSchedules.Single().Id);
        var originalScheduleId = group.WorkSchedules.Single().Id;

        Assert.Throws<DomainException>(() =>
            group.ReplaceSchedulesAndRotations(
                new List<CreateWorkScheduleDto> { CreateScheduleDto(group.Id) },
                new List<(int Position, int? WorkScheduleIndex)> { (1, 9) }));

        Assert.Single(group.WorkSchedules);
        Assert.Equal(originalScheduleId, group.WorkSchedules.Single().Id);
        Assert.Single(group.RotationEntries);
    }

    #endregion

    #region ReplaceRotationEntry Tests

    [Fact]
    public void ReplaceRotationEntry_UpdatesPositionAndSchedule()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var schedule = group.WorkSchedules.Single();
        group.AddRotationEntry(1, null);

        var updated = group.ReplaceRotationEntry(1, 3, schedule.Id);

        Assert.Equal(3, updated.Position);
        Assert.Equal(schedule.Id, updated.WorkScheduleId);
        Assert.Equal(RotationStatus.Work, updated.Status);
        Assert.Single(group.RotationEntries);
    }

    [Fact]
    public void ReplaceRotationEntry_ToDuplicatePosition_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        group.AddRotationEntry(1, null);
        group.AddRotationEntry(2, null);

        var exception = Assert.Throws<DomainException>(() => group.ReplaceRotationEntry(1, 2, null));

        Assert.Equal(EmployeeGroupErrors.DuplicateRotationPosition.Code, exception.Error.Code);
    }

    [Fact]
    public void ReplaceRotationEntry_InvalidPosition_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        group.AddRotationEntry(1, null);

        var exception = Assert.Throws<DomainException>(() => group.ReplaceRotationEntry(1, 0, null));

        Assert.Equal(RotationEntryErrors.InvalidPosition.Code, exception.Error.Code);
    }

    [Fact]
    public void ReplaceRotationEntry_NonExistentEntry_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var exception = Assert.Throws<DomainException>(() => group.ReplaceRotationEntry(5, 6, null));

        Assert.Equal(EmployeeGroupErrors.RotationEntryNotFound.Code, exception.Error.Code);
    }

    [Fact]
    public void ReplaceRotationEntry_ScheduleNotInGroup_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        group.AddRotationEntry(1, null);
        var otherGroup = CreateGroup(name: "Other", rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var dto = CreateScheduleDto(otherGroup.Id);
        otherGroup.AddWorkSchedule(dto);
        var scheduleFromOther = otherGroup.WorkSchedules.Single();

        var exception = Assert.Throws<DomainException>(() =>
            group.ReplaceRotationEntry(1, 1, scheduleFromOther.Id));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, exception.Error.Code);
    }

    #endregion

    #region UpdateDetails

    [Fact]
    public void UpdateDetails_WithRotationStartDate_UpdatesDateAndRaisesEvent()
    {
        var rotationStartDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var group = CreateGroup(rotationStartDate: rotationStartDate);

        var newRotationStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var oldDomainEvents = group.DomainEvents.ToList();

        group.UpdateDetails("New Name", true, "New Description", newRotationStartDate);

        Assert.Equal("New Name", group.Name);
        Assert.True(group.IsSecurity);
        Assert.Equal("New Description", group.Description);
        Assert.Equal(newRotationStartDate, group.RotationStartDate);

        var domainEvents = group.DomainEvents.Except(oldDomainEvents).ToList();
        var rotationEvent = domainEvents.OfType<EmployeeGroupRotationStartDateUpdatedDomainEvent>().SingleOrDefault();
        Assert.NotNull(rotationEvent);
        Assert.Equal(group.Id, rotationEvent.GroupId);
        Assert.Equal(rotationStartDate, rotationEvent.OldRotationStartDate);
    }

    [Fact]
    public void UpdateDetails_WithNullRotationStartDate_DoesNotUpdateDate()
    {
        var rotationStartDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var group = CreateGroup(rotationStartDate: rotationStartDate);

        group.UpdateDetails("New Name", true, "New Description", null);

        Assert.Equal("New Name", group.Name);
        Assert.True(group.IsSecurity);
        Assert.Equal("New Description", group.Description);
        Assert.Equal(rotationStartDate, group.RotationStartDate);

        var domainEvents = group.DomainEvents.ToList();
        var rotationEvent = domainEvents.OfType<EmployeeGroupRotationStartDateUpdatedDomainEvent>().SingleOrDefault();
        Assert.Null(rotationEvent);
    }

    [Fact]
    public void UpdateDetails_WithInvalidRotationStartDate_ThrowsDomainException()
    {
        var group = CreateGroup();

        var exception = Assert.Throws<DomainException>(() =>
            group.UpdateDetails("New Name", true, "New Description", new DateOnly()));

        Assert.Equal(EmployeeGroupErrors.RotationStartDateRequired.Code, exception.Error.Code);
    }

    #endregion

    #region UpdateWorkSchedule In-Use Tests

    [Fact]
    public void UpdateWorkSchedule_WhenReferencedByRotation_ThrowsDomainException()
    {
        var group = CreateGroup(rotationStartDate: DateOnly.FromDateTime(DateTime.UtcNow));
        var dto = CreateScheduleDto(group.Id);
        group.AddWorkSchedule(dto);
        var schedule = group.WorkSchedules.Single();
        group.AddRotationEntry(1, schedule.Id);

        var updateDto = new UpdateWorkScheduleDto(
            schedule.Id,
            group.Id,
            ShiftStartTime: new TimeOnly(9, 0),
            ShiftEndTime: new TimeOnly(17, 0),
            EndDayOffset: 0,
            BreakStartTime: new TimeOnly(12, 0),
            BreakEndTime: new TimeOnly(13, 0),
            AllowedCheckInLatenessMinutes: 10,
            AllowedCheckOutEarlinessMinutes: 10);

        var exception = Assert.Throws<DomainException>(() => group.UpdateWorkSchedule(updateDto));

        Assert.Equal(EmployeeGroupErrors.WorkScheduleInUse.Code, exception.Error.Code);
        Assert.Single(group.WorkSchedules);
    }

    #endregion
}