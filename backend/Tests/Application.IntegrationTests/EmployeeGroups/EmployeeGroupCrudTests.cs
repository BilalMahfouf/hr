using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Application.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Infrastructure.Presistance;

namespace Application.IntegrationTests.EmployeeGroups;

public sealed class EmployeeGroupCrudTests : EmployeeGroupsTestBase
{
    public EmployeeGroupCrudTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Create_ValidCommand_PersistsAndReturnsGroup()
    {
        using var scope = CreateScope();
        var handler = CreateCreateHandler(scope.ServiceProvider);

        var command = new CreateEmployeeGroupCommand(
            "Security Alpha",
            true,
            "Main security group",
            new DateOnly(2026, 1, 1),
            [ValidScheduleRequest()],
            [ValidWorkRotationRequest()]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Security Alpha", result.Value.Name);
        Assert.True(result.Value.IsSecurity);
        Assert.Equal("Main security group", result.Value.Description);
        Assert.Equal(new DateOnly(2026, 1, 1), result.Value.RotationStartDate);
        Assert.Single(result.Value.WorkSchedules);
        Assert.Single(result.Value.RotationEntries);

        var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
        var saved = await db.EmployeeGroups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == result.Value.Id);
        Assert.NotNull(saved);
        Assert.Equal("Security Alpha", saved!.Name);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsFailure()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeGroupRepository>();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        await SeedGroupAsync(repo, db, "Existing Group");

        var handler = CreateCreateHandler(scope.ServiceProvider);
        var command = new CreateEmployeeGroupCommand(
            "Existing Group",
            false,
            null,
            new DateOnly(2026, 1, 1),
            [ValidScheduleRequest()],
            [ValidWorkRotationRequest()]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NameAlreadyExists.Code, result.Error.Code);
    }

    [Fact]
    public async Task Create_EmptyName_ThrowsValidationException()
    {
        using var scope = CreateScope();
        var handler = CreateCreateHandler(scope.ServiceProvider);
        var command = new CreateEmployeeGroupCommand(
            "",
            false,
            null,
            new DateOnly(2026, 1, 1),
            [ValidScheduleRequest()],
            [ValidWorkRotationRequest()]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Create_EmptyRotationEntries_ThrowsValidationException()
    {
        using var scope = CreateScope();
        var handler = CreateCreateHandler(scope.ServiceProvider);
        var command = new CreateEmployeeGroupCommand(
            "Group",
            false,
            null,
            new DateOnly(2026, 1, 1),
            [ValidScheduleRequest()],
            []);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Create_InvalidScheduleReference_ThrowsValidationException()
    {
        using var scope = CreateScope();
        var handler = CreateCreateHandler(scope.ServiceProvider);
        var command = new CreateEmployeeGroupCommand(
            "Group",
            false,
            null,
            new DateOnly(2026, 1, 1),
            [ValidScheduleRequest()],
            [new CreateRotationEntryRequest(1, 99)]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Create_DuplicateRotationPositions_ThrowsValidationException()
    {
        using var scope = CreateScope();
        var handler = CreateCreateHandler(scope.ServiceProvider);
        var command = new CreateEmployeeGroupCommand(
            "Group",
            false,
            null,
            new DateOnly(2026, 1, 1),
            [ValidScheduleRequest()],
            [new CreateRotationEntryRequest(1, 0), new CreateRotationEntryRequest(1, 0)]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Update_ValidCommand_UpdatesFieldsAndPersists()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeGroupRepository>();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(repo, db);

        var handler = CreateUpdateHandler(scope.ServiceProvider);
        var command = new UpdateEmployeeGroupCommand(
            group.Id.Value,
            "Updated Group",
            true,
            "Updated description");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Group", result.Value.Name);
        Assert.True(result.Value.IsSecurity);
        Assert.Equal("Updated description", result.Value.Description);

        var saved = await db.EmployeeGroups.AsNoTracking()
            .SingleAsync(g => g.Id == group.Id);
        Assert.Equal("Updated Group", saved.Name);
    }

    [Fact]
    public async Task Update_DuplicateName_ReturnsFailure()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeGroupRepository>();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var groupA = await SeedGroupAsync(repo, db, "Group A");
        var groupB = await SeedGroupAsync(repo, db, "Group B");

        var handler = CreateUpdateHandler(scope.ServiceProvider);
        var command = new UpdateEmployeeGroupCommand(
            groupA.Id.Value,
            "Group B",
            null,
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NameAlreadyExists.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_SameName_NoConflict()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeGroupRepository>();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(repo, db, "Group A");

        var handler = CreateUpdateHandler(scope.ServiceProvider);
        var command = new UpdateEmployeeGroupCommand(
            group.Id.Value,
            "Group A",
            true,
            "new desc");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Group A", result.Value.Name);
        Assert.True(result.Value.IsSecurity);
    }

    [Fact]
    public async Task Update_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateUpdateHandler(scope.ServiceProvider);
        var command = new UpdateEmployeeGroupCommand(
            Guid.NewGuid(),
            "Name",
            null,
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Delete_ExistingGroup_RemovesGroupAndChildren()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeGroupRepository>();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAndRotationAsync(repo, db);

        var handler = CreateDeleteHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new DeleteEmployeeGroupCommand(group.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var deleted = await db.EmployeeGroups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == group.Id);
        Assert.Null(deleted);

        var schedules = await db.WorkSchedules.AsNoTracking()
            .Where(s => s.EmployeeGroupId == group.Id)
            .ToListAsync();
        Assert.Empty(schedules);

        var rotations = await db.RotationEntries.AsNoTracking()
            .Where(r => r.EmployeeGroupId == group.Id)
            .ToListAsync();
        Assert.Empty(rotations);
    }

    [Fact]
    public async Task Delete_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateDeleteHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new DeleteEmployeeGroupCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetById_ExistingGroup_ReturnsGroupWithChildren()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeGroupRepository>();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAndRotationAsync(repo, db);

        var handler = CreateGetByIdHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new GetEmployeeGroupByIdQuery(group.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(group.Id.Value, result.Value.Id);
        Assert.Equal(group.Name, result.Value.Name);
        Assert.Single(result.Value.WorkSchedules);
        Assert.Single(result.Value.RotationEntries);
    }

    [Fact]
    public async Task GetById_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateGetByIdHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new GetEmployeeGroupByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task GetAll_MultipleGroups_ReturnsOrderedByName()
    {
        using var scope = CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeGroupRepository>();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        await SeedGroupAsync(repo, db, "Charlie");
        await SeedGroupAsync(repo, db, "Alpha");
        await SeedGroupAsync(repo, db, "Bravo");

        var handler = CreateGetAllHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new GetAllEmployeeGroupsQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal("Alpha", result.Value[0].Name);
        Assert.Equal("Bravo", result.Value[1].Name);
        Assert.Equal("Charlie", result.Value[2].Name);
    }

    [Fact]
    public async Task GetAll_Empty_ReturnsEmptyList()
    {
        using var scope = CreateScope();
        var handler = CreateGetAllHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new GetAllEmployeeGroupsQuery(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
