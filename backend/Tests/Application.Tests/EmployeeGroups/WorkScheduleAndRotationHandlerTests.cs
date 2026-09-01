using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Application.EmployeeGroups;
using Modules.Employees.Application.EmployeeGroups.Rotations;
using Modules.Employees.Application.EmployeeGroups.WorkSchedules;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Employees.Infrastructure.Presistance;

namespace Application.Tests.EmployeeGroups;

public sealed class WorkScheduleAndRotationHandlerTests
{
    private static (EmployeeDbContext db, IEmployeeDbContext ctx) CreateDb()
    {
        var options = new DbContextOptionsBuilder<EmployeeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new EmployeeDbContext(options);
        return (db, db);
    }

    private static async Task<EmployeeGroup> CreateSeededGroupAsync(IEmployeeDbContext ctx)
    {
        var handler = new CreateEmployeeGroup.Handler(ctx, new CreateEmployeeGroup.Validator());
        var result = await handler.Handle(
            new CreateEmployeeGroupCommand(
                "GRP-001",
                "Day Shift",
                false,
                null,
                DateOnly.FromDateTime(DateTime.UtcNow),
                new List<CreateWorkScheduleRequest>
                {
                    new(new TimeOnly(8, 0), new TimeOnly(16, 0),
                        new TimeOnly(12, 0), new TimeOnly(13, 0), 0, 5, 5)
                },
                new List<CreateRotationEntryRequest> { new(1, 0) }),
            CancellationToken.None);

        return (await ctx.EmployeeGroups
            .Include(g => g.WorkSchedules)
            .Include(g => g.RotationEntries)
            .SingleAsync(g => g.Id == new EmployeeGroupId(result.Value.Id)))!;
    }

    private static async Task<EmployeeGroup> CreateSeededGroupWithoutRotationAsync(IEmployeeDbContext ctx)
    {
        var group = EmployeeGroup.Create(
            "GRP-001",
            "Day Shift",
            false,
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Test group");
        group.AddWorkSchedule(new CreateWorkScheduleDto(
            group.Id,
            new TimeOnly(8, 0),
            new TimeOnly(16, 0),
            0,
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            5,
            5));
        ctx.EmployeeGroups.Add(group);
        await ctx.SaveChangesAsync();

        return (await ctx.EmployeeGroups
            .Include(g => g.WorkSchedules)
            .Include(g => g.RotationEntries)
            .SingleAsync(g => g.Id == group.Id))!;
    }

    #region CreateWorkSchedule

    [Fact]
    public async Task CreateWorkSchedule_WhenValid_AddsSchedule()
    {
        var (db, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);

        var handler = new CreateWorkSchedule.Handler(ctx, new CreateWorkSchedule.Validator());
        var result = await handler.Handle(new CreateWorkScheduleCommand(
            group.Id.Value,
            new TimeOnly(15, 0), new TimeOnly(23, 0),
            new TimeOnly(19, 0), new TimeOnly(19, 30),
            0, 10, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await db.WorkSchedules.Where(s => s.EmployeeGroupId == group.Id).ToListAsync();
        Assert.Equal(2, updated.Count);
    }

    [Fact]
    public async Task CreateWorkSchedule_WhenGroupMissing_ReturnsNotFound()
    {
        var (_, ctx) = CreateDb();
        var handler = new CreateWorkSchedule.Handler(ctx, new CreateWorkSchedule.Validator());

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
        var (_, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);
        var handler = new CreateWorkSchedule.Handler(ctx, new CreateWorkSchedule.Validator());

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new CreateWorkScheduleCommand(
                group.Id.Value,
                new TimeOnly(18, 0), new TimeOnly(6, 0),
                new TimeOnly(12, 0), new TimeOnly(13, 0),
                0, 10, 10), CancellationToken.None));
    }

    #endregion

    #region UpdateWorkSchedule

    [Fact]
    public async Task UpdateWorkSchedule_WhenValid_UpdatesSchedule()
    {
        var (db, ctx) = CreateDb();
        var group = await CreateSeededGroupWithoutRotationAsync(ctx);
        var schedule = await db.WorkSchedules.SingleAsync(s => s.EmployeeGroupId == group.Id);

        var handler = new UpdateWorkSchedule.Handler(ctx, new UpdateWorkSchedule.Validator());
        var result = await handler.Handle(new UpdateWorkScheduleCommand(
            group.Id.Value, schedule.Id.Value,
            new TimeOnly(9, 0), new TimeOnly(17, 0),
            new TimeOnly(12, 0), new TimeOnly(13, 0),
            0, 10, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new TimeOnly(9, 0), result.Value.ShiftStartTime);
    }

    [Fact]
    public async Task UpdateWorkSchedule_WhenReferencedByRotation_ReturnsWorkScheduleInUse()
    {
        var (db, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);
        var schedule = await db.WorkSchedules.SingleAsync(s => s.EmployeeGroupId == group.Id);

        var handler = new UpdateWorkSchedule.Handler(ctx, new UpdateWorkSchedule.Validator());
        var result = await handler.Handle(new UpdateWorkScheduleCommand(
            group.Id.Value, schedule.Id.Value,
            new TimeOnly(9, 0), new TimeOnly(17, 0),
            new TimeOnly(12, 0), new TimeOnly(13, 0),
            0, 10, 10), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleInUse.Code, result.Error.Code);
    }

    [Fact]
    public async Task UpdateWorkSchedule_WhenScheduleMissing_ReturnsWorkScheduleNotFound()
    {
        var (_, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);

        var handler = new UpdateWorkSchedule.Handler(ctx, new UpdateWorkSchedule.Validator());
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
        var (db, ctx) = CreateDb();
        var group = await CreateSeededGroupWithoutRotationAsync(ctx);
        var schedule = await db.WorkSchedules.SingleAsync(s => s.EmployeeGroupId == group.Id);

        var handler = new DeleteWorkSchedule.Handler(ctx);
        var result = await handler.Handle(
            new DeleteWorkScheduleCommand(group.Id.Value, schedule.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(await db.WorkSchedules.Where(s => s.EmployeeGroupId == group.Id).ToListAsync());
    }

    [Fact]
    public async Task DeleteWorkSchedule_WhenReferencedByRotation_ReturnsWorkScheduleInUse()
    {
        var (db, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);
        var schedule = await db.WorkSchedules.SingleAsync(s => s.EmployeeGroupId == group.Id);

        var handler = new DeleteWorkSchedule.Handler(ctx);
        var result = await handler.Handle(
            new DeleteWorkScheduleCommand(group.Id.Value, schedule.Id.Value), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleInUse.Code, result.Error.Code);
    }

    #endregion

    #region Activate / Deactivate

    [Fact]
    public async Task ActivateWorkSchedule_WhenValid_ActivatesSchedule()
    {
        var (db, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);
        var schedule = await db.WorkSchedules.SingleAsync(s => s.EmployeeGroupId == group.Id);

        var handler = new ActivateWorkSchedule.Handler(ctx);
        var result = await handler.Handle(
            new ActivateWorkScheduleCommand(group.Id.Value, schedule.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task DeactivateWorkSchedule_WhenActive_DeactivatesSchedule()
    {
        var (db, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);
        var schedule = await db.WorkSchedules.SingleAsync(s => s.EmployeeGroupId == group.Id);

        var activateHandler = new ActivateWorkSchedule.Handler(ctx);
        await activateHandler.Handle(
            new ActivateWorkScheduleCommand(group.Id.Value, schedule.Id.Value), CancellationToken.None);

        var deactivateHandler = new DeactivateWorkSchedule.Handler(ctx);
        var result = await deactivateHandler.Handle(
            new DeactivateWorkScheduleCommand(group.Id.Value, schedule.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);
    }

    [Fact]
    public async Task ActivateWorkSchedule_WhenScheduleMissing_ReturnsWorkScheduleNotFound()
    {
        var (_, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);

        var handler = new ActivateWorkSchedule.Handler(ctx);
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
        var (db, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);

        var handler = new GetAllRotations.Handler(ctx);
        var result = await handler.Handle(
            new GetAllRotationsQuery(group.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(1, result.Value[0].Position);
        Assert.Equal("Work", result.Value[0].Status);
    }

    [Fact]
    public async Task CreateWorkRotation_WhenValid_AddsWorkEntry()
    {
        var (db, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);
        var schedule = await db.WorkSchedules.SingleAsync(s => s.EmployeeGroupId == group.Id);

        var handler = new CreateWorkRotation.Handler(ctx, new CreateWorkRotation.Validator());
        var result = await handler.Handle(new CreateWorkRotationCommand(
            group.Id.Value, 2, schedule.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Work", result.Value.Status);
        Assert.Equal(schedule.Id.Value, result.Value.WorkScheduleId);
    }

    [Fact]
    public async Task CreateWorkRotation_WhenScheduleNotInGroup_ReturnsWorkScheduleNotFound()
    {
        var (_, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);

        var handler = new CreateWorkRotation.Handler(ctx, new CreateWorkRotation.Validator());
        var result = await handler.Handle(new CreateWorkRotationCommand(
            group.Id.Value, 2, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task CreateRestRotation_WhenValid_AddsRestEntry()
    {
        var (_, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);

        var handler = new CreateRestRotation.Handler(ctx, new CreateRestRotation.Validator());
        var result = await handler.Handle(
            new CreateRestRotationCommand(group.Id.Value, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Rest", result.Value.Status);
        Assert.Null(result.Value.WorkScheduleId);
    }

    [Fact]
    public async Task CreateRestRotation_WhenPositionTaken_ReturnsDuplicatePosition()
    {
        var (_, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);

        var handler = new CreateRestRotation.Handler(ctx, new CreateRestRotation.Validator());
        var result = await handler.Handle(
            new CreateRestRotationCommand(group.Id.Value, 1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.DuplicateRotationPosition.Code, result.Error.Code);
    }

    [Fact]
    public async Task UpdateRotation_WhenValid_ChangesPositionAndSchedule()
    {
        var (db, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);
        var schedule = await db.WorkSchedules.SingleAsync(s => s.EmployeeGroupId == group.Id);

        var handler = new UpdateRotationPosition.Handler(ctx, new UpdateRotationPosition.Validator());
        var result = await handler.Handle(new UpdateRotationCommand(
            group.Id.Value, 1, 5, schedule.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Position);
        Assert.Equal("Work", result.Value.Status);
    }

    [Fact]
    public async Task UpdateRotation_WhenTargetPositionTaken_ReturnsDuplicatePosition()
    {
        var (db, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);
        var schedule = await db.WorkSchedules.SingleAsync(s => s.EmployeeGroupId == group.Id);

        var createHandler = new CreateRestRotation.Handler(ctx, new CreateRestRotation.Validator());
        await createHandler.Handle(new CreateRestRotationCommand(group.Id.Value, 2), CancellationToken.None);

        var handler = new UpdateRotationPosition.Handler(ctx, new UpdateRotationPosition.Validator());
        var result = await handler.Handle(new UpdateRotationCommand(
            group.Id.Value, 1, 2, schedule.Id.Value), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.DuplicateRotationPosition.Code, result.Error.Code);
    }

    [Fact]
    public async Task UpdateRotation_WhenEntryMissing_ReturnsRotationEntryNotFound()
    {
        var (_, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);

        var handler = new UpdateRotationPosition.Handler(ctx, new UpdateRotationPosition.Validator());
        var result = await handler.Handle(new UpdateRotationCommand(
            group.Id.Value, 9, 10, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.RotationEntryNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task DeleteRotation_WhenValid_RemovesEntry()
    {
        var (db, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);

        var createHandler = new CreateRestRotation.Handler(ctx, new CreateRestRotation.Validator());
        await createHandler.Handle(new CreateRestRotationCommand(group.Id.Value, 2), CancellationToken.None);

        var handler = new DeleteRotation.Handler(ctx);
        var result = await handler.Handle(
            new DeleteRotationCommand(group.Id.Value, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var remaining = await db.RotationEntries.Where(r => r.EmployeeGroupId == group.Id).ToListAsync();
        Assert.Single(remaining);
    }

    [Fact]
    public async Task DeleteRotation_WhenEntryMissing_ReturnsRotationEntryNotFound()
    {
        var (_, ctx) = CreateDb();
        var group = await CreateSeededGroupAsync(ctx);

        var handler = new DeleteRotation.Handler(ctx);
        var result = await handler.Handle(
            new DeleteRotationCommand(group.Id.Value, 5), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.RotationEntryNotFound.Code, result.Error.Code);
    }

    #endregion
}
