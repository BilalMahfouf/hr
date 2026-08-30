using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Application.EmployeeGroups;
using Modules.Employees.Application.EmployeeGroups.WorkSchedules;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Employees.Infrastructure.Presistance;

namespace Application.IntegrationTests.EmployeeGroups;

public sealed class WorkScheduleCrudTests : EmployeeGroupsTestBase
{
    public WorkScheduleCrudTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateWorkSchedule_ValidCommand_PersistsAndReturnsSchedule()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateWorkScheduleHandler(scope.ServiceProvider);
        var command = new CreateWorkScheduleCommand(
            group.Id.Value,
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(16)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(13)),
            0, 15, 15);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(group.Id.Value, result.Value.EmployeeGroupId);
        Assert.Equal(TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), result.Value.ShiftStartTime);
        Assert.Equal(TimeOnly.FromTimeSpan(TimeSpan.FromHours(16)), result.Value.ShiftEndTime);
        Assert.False(result.Value.IsActive);

        var saved = await db.WorkSchedules.AsNoTracking()
            .SingleAsync(s => s.Id == new WorkScheduleId(result.Value.Id));
        Assert.Equal(group.Id, saved.EmployeeGroupId);
    }

    [Fact]
    public async Task CreateWorkSchedule_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateWorkScheduleHandler(scope.ServiceProvider);
        var command = new CreateWorkScheduleCommand(
            Guid.NewGuid(),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(16)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(13)),
            0, 15, 15);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task CreateWorkSchedule_InvalidShift_ThrowsValidationException()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateWorkScheduleHandler(scope.ServiceProvider);
        var command = new CreateWorkScheduleCommand(
            group.Id.Value,
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(16)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(13)),
            0, 15, 15);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateWorkSchedule_NotReferenced_UpdatesAndPersists()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var (group, schedule) = await SeedGroupWithScheduleReturnScheduleAsync(db);

        var handler = CreateUpdateWorkScheduleHandler(scope.ServiceProvider);
        var command = new UpdateWorkScheduleCommand(
            group.Id.Value,
            schedule.Id.Value,
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(6)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(14)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(11)),
            0, 10, 10);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TimeOnly.FromTimeSpan(TimeSpan.FromHours(6)), result.Value.ShiftStartTime);
        Assert.Equal(TimeOnly.FromTimeSpan(TimeSpan.FromHours(14)), result.Value.ShiftEndTime);

        var saved = await db.WorkSchedules.AsNoTracking()
            .SingleAsync(s => s.EmployeeGroupId == group.Id);
        Assert.Equal(TimeOnly.FromTimeSpan(TimeSpan.FromHours(6)), saved.ShiftStartTime);
        Assert.Equal(TimeOnly.FromTimeSpan(TimeSpan.FromHours(14)), saved.ShiftEndTime);
    }

    [Fact]
    public async Task UpdateWorkSchedule_ReferencedByRotation_ReturnsWorkScheduleInUse()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAndRotationAsync(db);
        var schedule = group.WorkSchedules.First();

        var handler = CreateUpdateWorkScheduleHandler(scope.ServiceProvider);
        var command = new UpdateWorkScheduleCommand(
            group.Id.Value,
            schedule.Id.Value,
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(6)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(14)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(11)),
            0, 10, 10);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleInUse.Code, result.Error.Code);
    }

    [Fact]
    public async Task UpdateWorkSchedule_NonExistentSchedule_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateUpdateWorkScheduleHandler(scope.ServiceProvider);
        var command = new UpdateWorkScheduleCommand(
            group.Id.Value,
            Guid.NewGuid(),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(16)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)),
            TimeOnly.FromTimeSpan(TimeSpan.FromHours(13)),
            0, 15, 15);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task DeleteWorkSchedule_NotReferenced_RemovesAndPersists()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var (group, schedule) = await SeedGroupWithScheduleReturnScheduleAsync(db);

        var handler = CreateDeleteWorkScheduleHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new DeleteWorkScheduleCommand(group.Id.Value, schedule.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var deleted = await db.WorkSchedules.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == schedule.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteWorkSchedule_ReferencedByRotation_ReturnsWorkScheduleInUse()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAndRotationAsync(db);
        var schedule = group.WorkSchedules.First();

        var handler = CreateDeleteWorkScheduleHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new DeleteWorkScheduleCommand(group.Id.Value, schedule.Id.Value),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleInUse.Code, result.Error.Code);
    }

    [Fact]
    public async Task DeleteWorkSchedule_NonExistentSchedule_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateDeleteWorkScheduleHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new DeleteWorkScheduleCommand(group.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetWorkScheduleById_ExistingSchedule_ReturnsSchedule()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var (group, schedule) = await SeedGroupWithScheduleReturnScheduleAsync(db);

        var handler = CreateGetWorkScheduleByIdHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new GetWorkScheduleByIdQuery(group.Id.Value, schedule.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(schedule.Id.Value, result.Value.Id);
        Assert.Equal(group.Id.Value, result.Value.EmployeeGroupId);
        Assert.Equal(schedule.ShiftStartTime, result.Value.ShiftStartTime);
    }

    [Fact]
    public async Task GetWorkScheduleById_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateGetWorkScheduleByIdHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new GetWorkScheduleByIdQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetWorkScheduleById_NonExistentSchedule_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateGetWorkScheduleByIdHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new GetWorkScheduleByIdQuery(group.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task ActivateWorkSchedule_DeactivatedSchedule_ActivatesAndPersists()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var (group, schedule) = await SeedGroupWithScheduleReturnScheduleAsync(db);

        Assert.False(schedule.IsActive);

        var handler = CreateActivateWorkScheduleHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new ActivateWorkScheduleCommand(group.Id.Value, schedule.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);

        var saved = await db.WorkSchedules.AsNoTracking()
            .SingleAsync(s => s.Id == schedule.Id);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task ActivateWorkSchedule_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateActivateWorkScheduleHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new ActivateWorkScheduleCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task ActivateWorkSchedule_NonExistentSchedule_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateActivateWorkScheduleHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new ActivateWorkScheduleCommand(group.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task DeactivateWorkSchedule_ActiveSchedule_DeactivatesAndPersists()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var (group, schedule) = await SeedGroupWithScheduleReturnScheduleAsync(db);

        var activateHandler = CreateActivateWorkScheduleHandler(scope.ServiceProvider);
        await activateHandler.Handle(
            new ActivateWorkScheduleCommand(group.Id.Value, schedule.Id.Value),
            CancellationToken.None);

        var deactivateHandler = CreateDeactivateWorkScheduleHandler(scope.ServiceProvider);
        var result = await deactivateHandler.Handle(
            new DeactivateWorkScheduleCommand(group.Id.Value, schedule.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsActive);

        var saved = await db.WorkSchedules.AsNoTracking()
            .SingleAsync(s => s.Id == schedule.Id);
        Assert.False(saved.IsActive);
    }

    [Fact]
    public async Task DeactivateWorkSchedule_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateDeactivateWorkScheduleHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new DeactivateWorkScheduleCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task DeactivateWorkSchedule_NonExistentSchedule_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateDeactivateWorkScheduleHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new DeactivateWorkScheduleCommand(group.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.WorkScheduleNotFound.Code, result.Error.Code);
    }
}
