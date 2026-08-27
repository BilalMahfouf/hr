using FluentValidation;
using Moq;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Application.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.Domain.Common;

namespace Application.Tests.EmployeeGroups;

public sealed class EmployeeGroupCommandHandlerTests
{
    private readonly Mock<IEmployeeGroupRepository> _repoMock = new();
    private readonly Mock<IEmployeeDbContext> _dbMock = new();
    private readonly List<EmployeeGroup> _added = [];

    private static CreateWorkScheduleRequest Schedule(
        int startHour = 8, int endHour = 16) =>
        new(
            new TimeOnly(startHour, 0),
            new TimeOnly(endHour, 0),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            0,
            5,
            5);

    private static CreateEmployeeGroupCommand ValidCommand(string name = "Day Shift") =>
        new(
            name,
            false,
            null,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new List<CreateWorkScheduleRequest> { Schedule() },
            new List<CreateRotationEntryRequest>
            {
                new(1, 0),
                new(2, null),
            });

    private CreateEmployeeGroup.Handler CreateCreateHandler() =>
        new(_repoMock.Object, _dbMock.Object, new CreateEmployeeGroup.Validator());

    private void SetupAddCapture()
    {
        _repoMock
            .Setup(r => r.Add(It.IsAny<EmployeeGroup>()))
            .Callback<EmployeeGroup>(_added.Add);
        _dbMock
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    #region Create

    [Fact]
    public async Task Create_WhenValid_CreatesGroupWithSchedulesAndRotations()
    {
        SetupAddCapture();
        var handler = CreateCreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var group = Assert.Single(_added);
        Assert.Equal("Day Shift", group.Name);
        Assert.Single(group.WorkSchedules);
        Assert.Equal(2, group.NumberOfRotations);
        Assert.Equal(RotationStatus.Work, group.RotationEntries.First(e => e.Position == 1).Status);
        Assert.Equal(RotationStatus.Rest, group.RotationEntries.First(e => e.Position == 2).Status);
        Assert.Equal(group.WorkSchedules.Single().Id, group.RotationEntries.First(e => e.Position == 1).WorkScheduleId);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenNameAlreadyExists_ReturnsNameAlreadyExists()
    {
        _repoMock
            .Setup(r => r.GetByNameAsync("Day Shift", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmployeeGroup.Create("Day Shift", false, DateOnly.FromDateTime(DateTime.UtcNow)));

        var handler = CreateCreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NameAlreadyExists.Code, result.Error.Code);
        Assert.Empty(_added);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenRotationReferencesMissingSchedule_ThrowsValidationException()
    {
        SetupAddCapture();
        var handler = CreateCreateHandler();
        var command = new CreateEmployeeGroupCommand(
            "Day Shift",
            false,
            null,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new List<CreateWorkScheduleRequest> { Schedule() },
            new List<CreateRotationEntryRequest> { new(1, 5) });

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Create_WhenDuplicatePositions_ThrowsValidationException()
    {
        SetupAddCapture();
        var handler = CreateCreateHandler();
        var command = new CreateEmployeeGroupCommand(
            "Day Shift",
            false,
            null,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new List<CreateWorkScheduleRequest> { Schedule() },
            new List<CreateRotationEntryRequest> { new(1, 0), new(1, null) });

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Create_WhenNoRotations_ThrowsValidationException()
    {
        SetupAddCapture();
        var handler = CreateCreateHandler();
        var command = new CreateEmployeeGroupCommand(
            "Day Shift",
            false,
            null,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new List<CreateWorkScheduleRequest> { Schedule() },
            new List<CreateRotationEntryRequest>());

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Empty(_added);
    }

    [Fact]
    public async Task Create_WhenInvalidScheduleTimes_ThrowsValidationException()
    {
        SetupAddCapture();
        var handler = CreateCreateHandler();
        var command = new CreateEmployeeGroupCommand(
            "Day Shift",
            false,
            null,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new List<CreateWorkScheduleRequest> { Schedule(startHour: 18, endHour: 6) },
            new List<CreateRotationEntryRequest> { new(1, null) });

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Empty(_added);
    }

    #endregion

    #region GetById / GetAll

    [Fact]
    public async Task GetById_WhenGroupExists_ReturnsGroupWithChildren()
    {
        var group = EmployeeGroup.Create("Day Shift", false, DateOnly.FromDateTime(DateTime.UtcNow));
        group.AddWorkSchedule(new CreateWorkScheduleDto(
            group.Id, new TimeOnly(8, 0), new TimeOnly(16, 0), 0,
            new TimeOnly(12, 0), new TimeOnly(13, 0), 5, 5));
        group.AddRotationEntry(1, group.WorkSchedules.Single().Id);

        _repoMock
            .Setup(r => r.GetByIdWithDetailsAsync(group.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var handler = new GetEmployeeGroupById.Handler(_repoMock.Object);

        var result = await handler.Handle(new GetEmployeeGroupByIdQuery(group.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(group.Id.Value, result.Value.Id);
        Assert.Single(result.Value.WorkSchedules);
        Assert.Single(result.Value.RotationEntries);
        Assert.Equal("Work", result.Value.RotationEntries.Single().Status);
    }

    [Fact]
    public async Task GetById_WhenGroupMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repoMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<EmployeeGroupId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmployeeGroup?)null);

        var handler = new GetEmployeeGroupById.Handler(_repoMock.Object);

        var result = await handler.Handle(new GetEmployeeGroupByIdQuery(id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetAll_ReturnsAllGroups()
    {
        var group1 = EmployeeGroup.Create("A", false, DateOnly.FromDateTime(DateTime.UtcNow));
        var group2 = EmployeeGroup.Create("B", true, DateOnly.FromDateTime(DateTime.UtcNow));

        _repoMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmployeeGroup> { group1, group2 });

        var handler = new GetAllEmployeeGroups.Handler(_repoMock.Object);

        var result = await handler.Handle(new GetAllEmployeeGroupsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_WhenValid_UpdatesDetails()
    {
        var group = EmployeeGroup.Create("Day Shift", false, DateOnly.FromDateTime(DateTime.UtcNow));
        _repoMock
            .Setup(r => r.GetByIdWithDetailsAsync(group.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateEmployeeGroup.Handler(
            _repoMock.Object, _dbMock.Object, new UpdateEmployeeGroup.Validator());

        var result = await handler.Handle(
            new UpdateEmployeeGroupCommand(group.Id.Value, "Night Shift", true, "Updated"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Night Shift", group.Name);
        Assert.True(group.IsSecurity);
        Assert.Equal("Updated", group.Description);
    }

    [Fact]
    public async Task Update_WhenGroupMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repoMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<EmployeeGroupId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmployeeGroup?)null);

        var handler = new UpdateEmployeeGroup.Handler(
            _repoMock.Object, _dbMock.Object, new UpdateEmployeeGroup.Validator());

        var result = await handler.Handle(
            new UpdateEmployeeGroupCommand(id, "Night", null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_WhenNewNameTakenByAnotherGroup_ReturnsNameAlreadyExists()
    {
        var group = EmployeeGroup.Create("Day Shift", false, DateOnly.FromDateTime(DateTime.UtcNow));
        _repoMock
            .Setup(r => r.GetByIdWithDetailsAsync(group.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _repoMock
            .Setup(r => r.GetByNameAsync("Night Shift", It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmployeeGroup.Create("Night Shift", false, DateOnly.FromDateTime(DateTime.UtcNow)));

        var handler = new UpdateEmployeeGroup.Handler(
            _repoMock.Object, _dbMock.Object, new UpdateEmployeeGroup.Validator());

        var result = await handler.Handle(
            new UpdateEmployeeGroupCommand(group.Id.Value, "Night Shift", null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NameAlreadyExists.Code, result.Error.Code);
    }

    #endregion

    #region ReplaceSchedulesAndRotations

    [Fact]
    public async Task Replace_WhenValid_ReplacesEverything()
    {
        var group = EmployeeGroup.Create("Day Shift", false, DateOnly.FromDateTime(DateTime.UtcNow));
        _repoMock
            .Setup(r => r.GetByIdWithDetailsAsync(group.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ReplaceSchedulesAndRotations.Handler(
            _repoMock.Object, _dbMock.Object, new ReplaceSchedulesAndRotations.Validator());

        var command = new ReplaceSchedulesAndRotationsCommand(
            group.Id.Value,
            new List<CreateWorkScheduleRequest> { Schedule(), Schedule() },
            new List<CreateRotationEntryRequest> { new(1, 0), new(2, 1), new(3, null) });

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, group.WorkSchedules.Count);
        Assert.Equal(3, group.NumberOfRotations);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Replace_WhenGroupMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repoMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<EmployeeGroupId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmployeeGroup?)null);

        var handler = new ReplaceSchedulesAndRotations.Handler(
            _repoMock.Object, _dbMock.Object, new ReplaceSchedulesAndRotations.Validator());

        var result = await handler.Handle(
            new ReplaceSchedulesAndRotationsCommand(
                id,
                new List<CreateWorkScheduleRequest> { Schedule() },
                new List<CreateRotationEntryRequest> { new(1, null) }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Replace_WhenRotationIndexOutOfRange_ThrowsValidationException()
    {
        var group = EmployeeGroup.Create("Day Shift", false, DateOnly.FromDateTime(DateTime.UtcNow));
        _repoMock
            .Setup(r => r.GetByIdWithDetailsAsync(group.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var handler = new ReplaceSchedulesAndRotations.Handler(
            _repoMock.Object, _dbMock.Object, new ReplaceSchedulesAndRotations.Validator());

        var command = new ReplaceSchedulesAndRotationsCommand(
            group.Id.Value,
            new List<CreateWorkScheduleRequest> { Schedule() },
            new List<CreateRotationEntryRequest> { new(1, 3) });

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_WhenGroupExists_RemovesGroup()
    {
        var group = EmployeeGroup.Create("Day Shift", false, DateOnly.FromDateTime(DateTime.UtcNow));
        _repoMock
            .Setup(r => r.GetByIdWithDetailsAsync(group.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);
        _dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteEmployeeGroup.Handler(_repoMock.Object, _dbMock.Object);

        var result = await handler.Handle(
            new DeleteEmployeeGroupCommand(group.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.Remove(group), Times.Once);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenGroupMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repoMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<EmployeeGroupId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmployeeGroup?)null);

        var handler = new DeleteEmployeeGroup.Handler(_repoMock.Object, _dbMock.Object);

        var result = await handler.Handle(new DeleteEmployeeGroupCommand(id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    #endregion
}