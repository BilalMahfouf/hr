using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Application.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Employees.Infrastructure.Presistance;

namespace Application.Tests.EmployeeGroups;

public sealed class EmployeeGroupCommandHandlerTests
{
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

    private static (EmployeeDbContext db, IEmployeeDbContext ctx) CreateDb()
    {
        var options = new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new EmployeeDbContext(options);
        return (db, db);
    }

    private static CreateEmployeeGroup.Handler CreateCreateHandler(IEmployeeDbContext ctx) =>
        new(ctx, new CreateEmployeeGroup.Validator());

    #region Create

    [Fact]
    public async Task Create_WhenValid_CreatesGroupWithSchedulesAndRotations()
    {
        var (db, ctx) = CreateDb();
        var handler = CreateCreateHandler(ctx);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var group = await db.EmployeeGroups.Include(g => g.WorkSchedules).Include(g => g.RotationEntries).SingleAsync();
        Assert.Equal("Day Shift", group.Name);
        Assert.Equal("01", group.GroupNumber);
        Assert.Single(group.WorkSchedules);
        Assert.Equal(2, group.NumberOfRotations);
        Assert.Equal(RotationStatus.Work, group.RotationEntries.First(e => e.Position == 1).Status);
        Assert.Equal(RotationStatus.Rest, group.RotationEntries.First(e => e.Position == 2).Status);
        Assert.Equal(group.WorkSchedules.Single().Id, group.RotationEntries.First(e => e.Position == 1).WorkScheduleId);
    }

    [Fact]
    public async Task Create_MultipleGroups_AutoIncrementsGroupNumber()
    {
        var (db, ctx) = CreateDb();
        var handler = CreateCreateHandler(ctx);

        await handler.Handle(ValidCommand("First"), CancellationToken.None);
        await handler.Handle(ValidCommand("Second"), CancellationToken.None);
        await handler.Handle(ValidCommand("Third"), CancellationToken.None);

        var groups = await db.EmployeeGroups.OrderBy(g => g.GroupNumber).ToListAsync();
        Assert.Equal(3, groups.Count);
        Assert.Equal("01", groups[0].GroupNumber);
        Assert.Equal("02", groups[1].GroupNumber);
        Assert.Equal("03", groups[2].GroupNumber);
    }

    [Fact]
    public async Task Create_WhenNameAlreadyExists_ReturnsNameAlreadyExists()
    {
        var (_, ctx) = CreateDb();
        var handler = CreateCreateHandler(ctx);

        await handler.Handle(ValidCommand(name: "Day Shift"), CancellationToken.None);
        var result = await handler.Handle(ValidCommand(name: "Day Shift"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NameAlreadyExists.Code, result.Error.Code);
    }

    [Fact]
    public async Task Create_WhenRotationReferencesMissingSchedule_ThrowsValidationException()
    {
        var (_, ctx) = CreateDb();
        var handler = CreateCreateHandler(ctx);
        var command = new CreateEmployeeGroupCommand(
            "Day Shift",
            false,
            null,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new List<CreateWorkScheduleRequest> { Schedule() },
            new List<CreateRotationEntryRequest> { new(1, 5) });

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WhenDuplicatePositions_ThrowsValidationException()
    {
        var (_, ctx) = CreateDb();
        var handler = CreateCreateHandler(ctx);
        var command = new CreateEmployeeGroupCommand(
            "Day Shift",
            false,
            null,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new List<CreateWorkScheduleRequest> { Schedule() },
            new List<CreateRotationEntryRequest> { new(1, 0), new(1, null) });

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WhenNoRotations_ThrowsValidationException()
    {
        var (_, ctx) = CreateDb();
        var handler = CreateCreateHandler(ctx);
        var command = new CreateEmployeeGroupCommand(
            "Day Shift",
            false,
            null,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new List<CreateWorkScheduleRequest> { Schedule() },
            new List<CreateRotationEntryRequest>());

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WhenInvalidScheduleTimes_ThrowsValidationException()
    {
        var (_, ctx) = CreateDb();
        var handler = CreateCreateHandler(ctx);
        var command = new CreateEmployeeGroupCommand(
            "Day Shift",
            false,
            null,
            DateOnly.FromDateTime(DateTime.UtcNow),
            new List<CreateWorkScheduleRequest> { Schedule(startHour: 18, endHour: 6) },
            new List<CreateRotationEntryRequest> { new(1, null) });

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    #endregion

    #region GetById / GetAll

    [Fact]
    public async Task GetById_WhenGroupExists_ReturnsGroupWithChildren()
    {
        var (_, ctx) = CreateDb();
        var createHandler = CreateCreateHandler(ctx);
        var createResult = await createHandler.Handle(ValidCommand(), CancellationToken.None);

        var handler = new GetEmployeeGroupById.Handler(ctx);
        var result = await handler.Handle(new GetEmployeeGroupByIdQuery(createResult.Value.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(createResult.Value.Id, result.Value.Id);
        Assert.Single(result.Value.WorkSchedules);
        Assert.Equal(2, result.Value.RotationEntries.Count);
        Assert.Equal("Work", result.Value.RotationEntries.First(e => e.Position == 1).Status);
        Assert.Equal("Rest", result.Value.RotationEntries.First(e => e.Position == 2).Status);
    }

    [Fact]
    public async Task GetById_WhenGroupMissing_ReturnsNotFound()
    {
        var (_, ctx) = CreateDb();
        var handler = new GetEmployeeGroupById.Handler(ctx);

        var result = await handler.Handle(new GetEmployeeGroupByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetAll_ReturnsAllGroups()
    {
        var (_, ctx) = CreateDb();
        var createHandler = CreateCreateHandler(ctx);
        await createHandler.Handle(ValidCommand("A"), CancellationToken.None);
        await createHandler.Handle(ValidCommand("B"), CancellationToken.None);

        var handler = new GetAllEmployeeGroups.Handler(ctx);
        var result = await handler.Handle(new GetAllEmployeeGroupsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_WhenValid_UpdatesDetails()
    {
        var (_, ctx) = CreateDb();
        var createHandler = CreateCreateHandler(ctx);
        var createResult = await createHandler.Handle(ValidCommand(), CancellationToken.None);

        var handler = new UpdateEmployeeGroup.Handler(ctx, new UpdateEmployeeGroup.Validator());
        var result = await handler.Handle(
            new UpdateEmployeeGroupCommand(createResult.Value.Id, "Night Shift", true, "Updated"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Night Shift", result.Value.Name);
        Assert.True(result.Value.IsSecurity);
        Assert.Equal("Updated", result.Value.Description);
    }

    [Fact]
    public async Task Update_WhenGroupMissing_ReturnsNotFound()
    {
        var (_, ctx) = CreateDb();
        var handler = new UpdateEmployeeGroup.Handler(ctx, new UpdateEmployeeGroup.Validator());

        var result = await handler.Handle(
            new UpdateEmployeeGroupCommand(Guid.NewGuid(), "Night", null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_WhenNewNameTakenByAnotherGroup_ReturnsNameAlreadyExists()
    {
        var (_, ctx) = CreateDb();
        var createHandler = CreateCreateHandler(ctx);
        await createHandler.Handle(ValidCommand("Day Shift"), CancellationToken.None);
        var createResult2 = await createHandler.Handle(ValidCommand("Night Shift"), CancellationToken.None);

        var handler = new UpdateEmployeeGroup.Handler(ctx, new UpdateEmployeeGroup.Validator());
        var result = await handler.Handle(
            new UpdateEmployeeGroupCommand(createResult2.Value.Id, "Day Shift", null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NameAlreadyExists.Code, result.Error.Code);
    }

    #endregion

    #region ReplaceSchedulesAndRotations

    [Fact]
    public async Task Replace_WhenValid_ReplacesEverything()
    {
        var (_, ctx) = CreateDb();
        var createHandler = CreateCreateHandler(ctx);
        var createResult = await createHandler.Handle(ValidCommand(), CancellationToken.None);

        var handler = new ReplaceSchedulesAndRotations.Handler(ctx, new ReplaceSchedulesAndRotations.Validator());
        var command = new ReplaceSchedulesAndRotationsCommand(
            createResult.Value.Id,
            new List<CreateWorkScheduleRequest> { Schedule(), Schedule() },
            new List<CreateRotationEntryRequest> { new(1, 0), new(2, 1), new(3, null) });

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.WorkSchedules.Count);
        Assert.Equal(3, result.Value.NumberOfRotations);
    }

    [Fact]
    public async Task Replace_WhenGroupMissing_ReturnsNotFound()
    {
        var (_, ctx) = CreateDb();
        var handler = new ReplaceSchedulesAndRotations.Handler(ctx, new ReplaceSchedulesAndRotations.Validator());

        var result = await handler.Handle(
            new ReplaceSchedulesAndRotationsCommand(
                Guid.NewGuid(),
                new List<CreateWorkScheduleRequest> { Schedule() },
                new List<CreateRotationEntryRequest> { new(1, null) }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Replace_WhenRotationIndexOutOfRange_ThrowsValidationException()
    {
        var (_, ctx) = CreateDb();
        var createHandler = CreateCreateHandler(ctx);
        var createResult = await createHandler.Handle(ValidCommand(), CancellationToken.None);

        var handler = new ReplaceSchedulesAndRotations.Handler(ctx, new ReplaceSchedulesAndRotations.Validator());
        var command = new ReplaceSchedulesAndRotationsCommand(
            createResult.Value.Id,
            new List<CreateWorkScheduleRequest> { Schedule() },
            new List<CreateRotationEntryRequest> { new(1, 3) });

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_WhenGroupExists_RemovesGroup()
    {
        var (db, ctx) = CreateDb();
        var createHandler = CreateCreateHandler(ctx);
        var createResult = await createHandler.Handle(ValidCommand(), CancellationToken.None);

        var handler = new DeleteEmployeeGroup.Handler(ctx);
        var result = await handler.Handle(
            new DeleteEmployeeGroupCommand(createResult.Value.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(await db.EmployeeGroups.ToListAsync());
    }

    [Fact]
    public async Task Delete_WhenGroupMissing_ReturnsNotFound()
    {
        var (_, ctx) = CreateDb();
        var handler = new DeleteEmployeeGroup.Handler(ctx);

        var result = await handler.Handle(new DeleteEmployeeGroupCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    #endregion
}
