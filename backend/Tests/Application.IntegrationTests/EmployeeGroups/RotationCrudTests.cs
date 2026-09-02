using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Application.EmployeeGroups;
using Modules.Employees.Application.EmployeeGroups.Rotations;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Infrastructure.Presistance;

namespace Application.IntegrationTests.EmployeeGroups;

public sealed class RotationCrudTests : EmployeeGroupsTestBase
{
    public RotationCrudTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateWorkRotation_ValidCommand_PersistsAndReturnsEntry()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAsync(db);
        var schedule = group.WorkSchedules.First();

        var handler = CreateWorkRotationHandler(scope.ServiceProvider);
        var command = new CreateWorkRotationCommand(group.Id.Value, 1, schedule.Id.Value);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Position);
        Assert.Equal(schedule.Id.Value, result.Value.WorkScheduleId);
        Assert.Equal("Work", result.Value.Status);

        var saved = await db.RotationEntries.AsNoTracking()
            .SingleAsync(r => r.EmployeeGroupId == group.Id && r.Position == 1);
        Assert.Equal(schedule.Id, saved.WorkScheduleId);
    }

    [Fact]
    public async Task CreateWorkRotation_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateWorkRotationHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateWorkRotationCommand(Guid.NewGuid(), 1, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task CreateWorkRotation_DuplicatePosition_ReturnsDuplicateRotationPosition()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAsync(db);
        var schedule = group.WorkSchedules.First();

        var handler = CreateWorkRotationHandler(scope.ServiceProvider);
        await handler.Handle(
            new CreateWorkRotationCommand(group.Id.Value, 1, schedule.Id.Value),
            CancellationToken.None);

        var result = await handler.Handle(
            new CreateWorkRotationCommand(group.Id.Value, 1, schedule.Id.Value),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.DuplicateRotationPosition.Code, result.Error.Code);
    }

    [Fact]
    public async Task CreateWorkRotation_NonExistentSchedule_ReturnsWorkScheduleNotFound()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateWorkRotationHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new CreateWorkRotationCommand(group.Id.Value, 1, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task CreateWorkRotation_PositionZero_ThrowsValidationException()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAsync(db);
        var schedule = group.WorkSchedules.First();

        var handler = CreateWorkRotationHandler(scope.ServiceProvider);
        var command = new CreateWorkRotationCommand(group.Id.Value, 0, schedule.Id.Value);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRestRotation_ValidCommand_PersistsAndReturnsEntry()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateRestRotationHandler(scope.ServiceProvider);
        var command = new CreateRestRotationCommand(group.Id.Value, 1);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Position);
        Assert.Null(result.Value.WorkScheduleId);
        Assert.Equal("Rest", result.Value.Status);

        var saved = await db.RotationEntries.AsNoTracking()
            .SingleAsync(r => r.EmployeeGroupId == group.Id && r.Position == 1);
        Assert.Null(saved.WorkScheduleId);
    }

    [Fact]
    public async Task CreateRestRotation_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateRestRotationHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateRestRotationCommand(Guid.NewGuid(), 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task CreateRestRotation_DuplicatePosition_ReturnsDuplicateRotationPosition()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateRestRotationHandler(scope.ServiceProvider);
        await handler.Handle(
            new CreateRestRotationCommand(group.Id.Value, 1),
            CancellationToken.None);

        var result = await handler.Handle(
            new CreateRestRotationCommand(group.Id.Value, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.DuplicateRotationPosition.Code, result.Error.Code);
    }

    [Fact]
    public async Task UpdateRotationPosition_ChangePositionAndType_PersistsAndReturnsEntry()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAndRotationAsync(db);
        var existingEntry = group.RotationEntries.First();
        var schedule = group.WorkSchedules.First();

        var handler = CreateUpdateRotationHandler(scope.ServiceProvider);
        var command = new UpdateRotationCommand(
            group.Id.Value,
            existingEntry.Position,
            2,
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Position);
        Assert.Equal("Rest", result.Value.Status);

        var saved = await db.RotationEntries.AsNoTracking()
            .SingleAsync(r => r.EmployeeGroupId == group.Id && r.Position == 2);
        Assert.Null(saved.WorkScheduleId);

        var oldEntry = await db.RotationEntries.AsNoTracking()
            .FirstOrDefaultAsync(r => r.EmployeeGroupId == group.Id && r.Position == 1);
        Assert.Null(oldEntry);
    }

    [Fact]
    public async Task UpdateRotationPosition_NonExistentEntry_ReturnsRotationEntryNotFound()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateUpdateRotationHandler(scope.ServiceProvider);
        var command = new UpdateRotationCommand(group.Id.Value, 1, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.RotationEntryNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task UpdateRotationPosition_DuplicateTargetPosition_ReturnsDuplicateRotationPosition()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAsync(db);
        var schedule = group.WorkSchedules.First();

        group.AddRotationEntry(1, schedule.Id);
        group.AddRotationEntry(2, null);
        await db.SaveChangesAsync();

        var handler = CreateUpdateRotationHandler(scope.ServiceProvider);
        var command = new UpdateRotationCommand(group.Id.Value, 1, 2, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.DuplicateRotationPosition.Code, result.Error.Code);
    }

    [Fact]
    public async Task UpdateRotationPosition_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateUpdateRotationHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new UpdateRotationCommand(Guid.NewGuid(), 1, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task UpdateRotationPosition_NewPositionBelowOne_ThrowsValidationException()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAndRotationAsync(db);

        var handler = CreateUpdateRotationHandler(scope.ServiceProvider);
        var command = new UpdateRotationCommand(group.Id.Value, 1, 0, null);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteRotation_ExistingEntry_RemovesAndPersists()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAndRotationAsync(db);

        var handler = CreateDeleteRotationHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new DeleteRotationCommand(group.Id.Value, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var deleted = await db.RotationEntries.AsNoTracking()
            .FirstOrDefaultAsync(r => r.EmployeeGroupId == group.Id && r.Position == 1);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteRotation_NonExistentEntry_ReturnsRotationEntryNotFound()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateDeleteRotationHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new DeleteRotationCommand(group.Id.Value, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.RotationEntryNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task DeleteRotation_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateDeleteRotationHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new DeleteRotationCommand(Guid.NewGuid(), 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetAllRotations_ExistingGroup_ReturnsOrderedByPosition()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAsync(db);
        var schedule = group.WorkSchedules.First();

        group.AddRotationEntry(3, null);
        group.AddRotationEntry(1, schedule.Id);
        group.AddRotationEntry(2, schedule.Id);
        await db.SaveChangesAsync();

        var handler = CreateGetAllRotationsHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new GetAllRotationsQuery(group.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal(1, result.Value[0].Position);
        Assert.Equal(2, result.Value[1].Position);
        Assert.Equal(3, result.Value[2].Position);
    }

    [Fact]
    public async Task GetAllRotations_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateGetAllRotationsHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new GetAllRotationsQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetAllRotations_EmptyGroup_ReturnsEmptyList()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateGetAllRotationsHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new GetAllRotationsQuery(group.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
