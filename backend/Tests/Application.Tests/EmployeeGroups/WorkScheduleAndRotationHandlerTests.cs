using FluentValidation;
using Moq;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Application.EmployeeGroups;
using Modules.Employees.Application.EmployeeGroups.Rotations;
using Modules.Employees.Application.EmployeeGroups.WorkSchedules;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;

namespace Application.Tests.EmployeeGroups;

public sealed class WorkScheduleAndRotationHandlerTests
{
    private readonly Mock<IEmployeeGroupRepository> _repoMock = new();
    private readonly Mock<IEmployeeDbContext> _dbMock = new();

    private static EmployeeGroup CreateSeededGroup(out WorkSchedule schedule)
    {
        var group = EmployeeGroup.Create("Day Shift", false, DateOnly.FromDateTime(DateTime.UtcNow));
        group.AddWorkSchedule(new CreateWorkScheduleDto(
            group.Id, new TimeOnly(8, 0), new TimeOnly(16, 0), 0,
            new TimeOnly(12, 0), new TimeOnly(13, 0), 5, 5));
        schedule = group.WorkSchedules.Single();
        return group;
    }

    private void SetupGroup(EmployeeGroup group)
    {
        _repoMock
            .Setup(r => r.GetByIdWithDetailsAsync(group.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    #region CreateWorkSchedule

    [Fact]
    public async Task CreateWorkSchedule_WhenValid_AddsSchedule()
    {
        var group = CreateSeededGroup(out _);
        SetupGroup(group);

        var handler = new CreateWorkSchedule.Handler(
            _repoMock.Object, _dbMock.Object, new CreateWorkSchedule.Validator());

        var result = await handler.Handle(new CreateWorkScheduleCommand(
            group.Id.Value,
            new TimeOnly(15, 0), new TimeOnly(23, 0),
            new TimeOnly(19, 0), new TimeOnly(19, 30),
            0, 10, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, group.WorkSchedules.Count);
        Assert.False(result.Value.IsActive);
    }

    [Fact]
    public async Task CreateWorkSchedule_WhenGroupMissing_ReturnsNotFound()
    {
        var handler = new CreateWorkSchedule.Handler(
            _repoMock.Object, _dbMock.Object, new CreateWorkSchedule.Validator());

        var result = await handler.Handle(new CreateWorkScheduleCommand(
            Guid.NewGuid(),
            new TimeOnly(15, 0), new TimeOnly(23, 0),
            new TimeOnly(19, 0), new TimeOnly(19, 30),
            0, 10, 10), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task CreateWorkSchedule_WhenInvalidTimes_ThrowsValidationException()
    {
        var group = CreateSeededGroup(out _);
        SetupGroup(group);

        var handler = new CreateWorkSchedule.Handler(
            _repoMock.Object, _dbMock.Object, new CreateWorkSchedule.Validator());

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new CreateWorkScheduleCommand(
                group.Id.Value,
                new TimeOnly(18, 0), new TimeOnly(6, 0),
                new TimeOnly(12, 0), new TimeOnly(13, 0),
                0, 10, 10), CancellationToken.None));

        Assert.Single(group.WorkSchedules);
    }

    #endregion

    #region UpdateWorkSchedule

    [Fact]
    public async Task UpdateWorkSchedule_WhenValid_UpdatesSchedule()
    {
        var group = CreateSeededGroup(out var schedule);
        SetupGroup(group);

        var handler = new UpdateWorkSchedule.Handler(
            _repoMock.Object, _dbMock.Object, new UpdateWorkSchedule.Validator());

        var result = await handler.Handle(new UpdateWorkScheduleCommand(
            group.Id.Value, schedule.Id.Value,
            new TimeOnly(9, 0), new TimeOnly(17, 0),
            new TimeOnly(12, 0), new TimeOnly(13, 0),
            0, 10, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(group.WorkSchedules);
        Assert.Equal(new TimeOnly(9, 0), group.WorkSchedules.Single().ShiftStartTime);
    }

    [Fact]
    public async Task UpdateWorkSchedule_WhenReferencedByRotation_ReturnsWorkScheduleInUse()
    {
        var group = CreateSeededGroup(out var schedule);
        group.AddRotationEntry(1, schedule.Id);
        SetupGroup(group);

        var handler = new UpdateWorkSchedule.Handler(
            _repoMock.Object, _dbMock.Object, new UpdateWorkSchedule.Validator());

        var result = await handler.Handle(new UpdateWorkScheduleCommand(
            group.Id.Value, schedule.Id.Value,
            new TimeOnly(9, 0), new TimeOnly(17, 0),
            new TimeOnly(12, 0), new TimeOnly(13, 0),
            0, 10, 10), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleInUse.Code, result.Error.Code);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateWorkSchedule_WhenScheduleMissing_ReturnsWorkScheduleNotFound()
    {
        var group = CreateSeededGroup(out _);
        SetupGroup(group);

        var handler = new UpdateWorkSchedule.Handler(
            _repoMock.Object, _dbMock.Object, new UpdateWorkSchedule.Validator());

        var result = await handler.Handle(new UpdateWorkScheduleCommand(
            group.Id.Value, Guid.NewGuid(),
            new TimeOnly(9, 0), new TimeOnly(17, 0),
            new TimeOnly(12, 0), new TimeOnly(13, 0),
            0, 10, 10), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, result.Error.Code);
    }

    #endregion

    #region DeleteWorkSchedule

    [Fact]
    public async Task DeleteWorkSchedule_WhenValid_RemovesSchedule()
    {
        var group = CreateSeededGroup(out var schedule);
        SetupGroup(group);

        var handler = new DeleteWorkSchedule.Handler(_repoMock.Object, _dbMock.Object);

        var result = await handler.Handle(
            new DeleteWorkScheduleCommand(group.Id.Value, schedule.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(group.WorkSchedules);
    }

    [Fact]
    public async Task DeleteWorkSchedule_WhenReferencedByRotation_ReturnsWorkScheduleInUse()
    {
        var group = CreateSeededGroup(out var schedule);
        group.AddRotationEntry(1, schedule.Id);
        SetupGroup(group);

        var handler = new DeleteWorkSchedule.Handler(_repoMock.Object, _dbMock.Object);

        var result = await handler.Handle(
            new DeleteWorkScheduleCommand(group.Id.Value, schedule.Id.Value), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleInUse.Code, result.Error.Code);
        Assert.Single(group.WorkSchedules);
    }

    #endregion

    #region Activate / Deactivate

    [Fact]
    public async Task ActivateWorkSchedule_WhenValid_ActivatesSchedule()
    {
        var group = CreateSeededGroup(out var schedule);
        SetupGroup(group);

        var handler = new ActivateWorkSchedule.Handler(_repoMock.Object, _dbMock.Object);

        var result = await handler.Handle(
            new ActivateWorkScheduleCommand(group.Id.Value, schedule.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task DeactivateWorkSchedule_WhenActive_DeactivatesSchedule()
    {
        var group = CreateSeededGroup(out var schedule);
        group.ActivateWorkSchedule(schedule.Id);
        SetupGroup(group);

        var handler = new DeactivateWorkSchedule.Handler(_repoMock.Object, _dbMock.Object);

        var result = await handler.Handle(
            new DeactivateWorkScheduleCommand(group.Id.Value, schedule.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
    }

    [Fact]
    public async Task ActivateWorkSchedule_WhenScheduleMissing_ReturnsWorkScheduleNotFound()
    {
        var group = CreateSeededGroup(out _);
        SetupGroup(group);

        var handler = new ActivateWorkSchedule.Handler(_repoMock.Object, _dbMock.Object);

        var result = await handler.Handle(
            new ActivateWorkScheduleCommand(group.Id.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, result.Error.Code);
    }

    #endregion

    #region Rotations

    [Fact]
    public async Task GetRotations_ReturnsEntriesOrderedByPosition()
    {
        var group = CreateSeededGroup(out var schedule);
        group.AddRotationEntry(2, null);
        group.AddRotationEntry(1, schedule.Id);
        SetupGroup(group);

        var handler = new GetAllRotations.Handler(_repoMock.Object);

        var result = await handler.Handle(
            new GetAllRotationsQuery(group.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(1, result.Value[0].Position);
        Assert.Equal("Work", result.Value[0].Status);
        Assert.Equal(2, result.Value[1].Position);
        Assert.Equal("Rest", result.Value[1].Status);
    }

    [Fact]
    public async Task CreateWorkRotation_WhenValid_AddsWorkEntry()
    {
        var group = CreateSeededGroup(out var schedule);
        SetupGroup(group);

        var handler = new CreateWorkRotation.Handler(
            _repoMock.Object, _dbMock.Object, new CreateWorkRotation.Validator());

        var result = await handler.Handle(new CreateWorkRotationCommand(
            group.Id.Value, 1, schedule.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Work", result.Value.Status);
        Assert.Equal(schedule.Id.Value, result.Value.WorkScheduleId);
    }

    [Fact]
    public async Task CreateWorkRotation_WhenScheduleNotInGroup_ReturnsWorkScheduleNotFound()
    {
        var group = CreateSeededGroup(out _);
        SetupGroup(group);

        var handler = new CreateWorkRotation.Handler(
            _repoMock.Object, _dbMock.Object, new CreateWorkRotation.Validator());

        var result = await handler.Handle(new CreateWorkRotationCommand(
            group.Id.Value, 1, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task CreateRestRotation_WhenValid_AddsRestEntry()
    {
        var group = CreateSeededGroup(out _);
        SetupGroup(group);

        var handler = new CreateRestRotation.Handler(
            _repoMock.Object, _dbMock.Object, new CreateRestRotation.Validator());

        var result = await handler.Handle(
            new CreateRestRotationCommand(group.Id.Value, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Rest", result.Value.Status);
        Assert.Null(result.Value.WorkScheduleId);
    }

    [Fact]
    public async Task CreateRestRotation_WhenPositionTaken_ReturnsDuplicatePosition()
    {
        var group = CreateSeededGroup(out _);
        group.AddRotationEntry(1, null);
        SetupGroup(group);

        var handler = new CreateRestRotation.Handler(
            _repoMock.Object, _dbMock.Object, new CreateRestRotation.Validator());

        var result = await handler.Handle(
            new CreateRestRotationCommand(group.Id.Value, 1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.DuplicateRotationPosition.Code, result.Error.Code);
    }

    [Fact]
    public async Task UpdateRotation_WhenValid_ChangesPositionAndSchedule()
    {
        var group = CreateSeededGroup(out var schedule);
        group.AddRotationEntry(1, null);
        SetupGroup(group);

        var handler = new UpdateRotationPosition.Handler(
            _repoMock.Object, _dbMock.Object, new UpdateRotationPosition.Validator());

        var result = await handler.Handle(new UpdateRotationCommand(
            group.Id.Value, 1, 5, schedule.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Position);
        Assert.Equal("Work", result.Value.Status);
        Assert.Single(group.RotationEntries);
    }

    [Fact]
    public async Task UpdateRotation_WhenTargetPositionTaken_ReturnsDuplicatePosition()
    {
        var group = CreateSeededGroup(out var schedule);
        group.AddRotationEntry(1, null);
        group.AddRotationEntry(2, null);
        SetupGroup(group);

        var handler = new UpdateRotationPosition.Handler(
            _repoMock.Object, _dbMock.Object, new UpdateRotationPosition.Validator());

        var result = await handler.Handle(new UpdateRotationCommand(
            group.Id.Value, 1, 2, schedule.Id.Value), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.DuplicateRotationPosition.Code, result.Error.Code);
    }

    [Fact]
    public async Task UpdateRotation_WhenEntryMissing_ReturnsRotationEntryNotFound()
    {
        var group = CreateSeededGroup(out _);
        SetupGroup(group);

        var handler = new UpdateRotationPosition.Handler(
            _repoMock.Object, _dbMock.Object, new UpdateRotationPosition.Validator());

        var result = await handler.Handle(new UpdateRotationCommand(
            group.Id.Value, 9, 10, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.RotationEntryNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task DeleteRotation_WhenValid_RemovesEntry()
    {
        var group = CreateSeededGroup(out _);
        group.AddRotationEntry(1, null);
        SetupGroup(group);

        var handler = new DeleteRotation.Handler(_repoMock.Object, _dbMock.Object);

        var result = await handler.Handle(
            new DeleteRotationCommand(group.Id.Value, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(group.RotationEntries);
    }

    [Fact]
    public async Task DeleteRotation_WhenEntryMissing_ReturnsRotationEntryNotFound()
    {
        var group = CreateSeededGroup(out _);
        SetupGroup(group);

        var handler = new DeleteRotation.Handler(_repoMock.Object, _dbMock.Object);

        var result = await handler.Handle(
            new DeleteRotationCommand(group.Id.Value, 5), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.RotationEntryNotFound.Code, result.Error.Code);
    }

    #endregion
}