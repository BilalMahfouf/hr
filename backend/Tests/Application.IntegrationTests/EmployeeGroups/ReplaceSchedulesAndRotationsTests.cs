using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Application.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Infrastructure.Presistance;

namespace Application.IntegrationTests.EmployeeGroups;

public sealed class ReplaceSchedulesAndRotationsTests : EmployeeGroupsTestBase
{
    public ReplaceSchedulesAndRotationsTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ReplaceSchedulesAndRotations_ValidCommand_ReplacesAllAndPersists()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupWithScheduleAndRotationAsync(db);

        var handler = CreateReplaceHandler(scope.ServiceProvider);
        var command = new ReplaceSchedulesAndRotationsCommand(
            group.Id.Value,
            [
                new CreateWorkScheduleRequest(
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours(6)),
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours(14)),
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours(11)),
                    0, 10, 10),
                new CreateWorkScheduleRequest(
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours(18)),
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours(14)),
                    TimeOnly.FromTimeSpan(TimeSpan.FromHours(15)),
                    0, 5, 5)
            ],
            [
                new CreateRotationEntryRequest(1, 0),
                new CreateRotationEntryRequest(2, null),
                new CreateRotationEntryRequest(3, 1)
            ]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.WorkSchedules.Count);
        Assert.Equal(3, result.Value.RotationEntries.Count);

        var saved = await db.EmployeeGroups.AsNoTracking()
            .Include(g => g.WorkSchedules)
            .Include(g => g.RotationEntries)
            .SingleAsync(g => g.Id == group.Id);
        Assert.Equal(2, saved.WorkSchedules.Count);
        Assert.Equal(3, saved.RotationEntries.Count);
    }

    [Fact]
    public async Task ReplaceSchedulesAndRotations_NonExistentGroup_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var handler = CreateReplaceHandler(scope.ServiceProvider);
        var command = new ReplaceSchedulesAndRotationsCommand(
            Guid.NewGuid(),
            [ValidScheduleRequest()],
            [ValidWorkRotationRequest()]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeGroupErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task ReplaceSchedulesAndRotations_InvalidScheduleReference_ThrowsValidationException()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IEmployeeDbContext>();
        var group = await SeedGroupAsync(db);

        var handler = CreateReplaceHandler(scope.ServiceProvider);
        var command = new ReplaceSchedulesAndRotationsCommand(
            group.Id.Value,
            [ValidScheduleRequest()],
            [new CreateRotationEntryRequest(1, 99)]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
