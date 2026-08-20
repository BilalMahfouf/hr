using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Modules.Attendence.Application.Machines;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.Machines;

namespace Application.IntegrationTests.Attendence;

public sealed class MachineCrudTests : AttendenceTestBase
{
    public MachineCrudTests(PostgresFixture fixture) : base(fixture)
    {
    }

    private static (GetMachineById.QueryHandler, ActivateMachine.CommandHandler, DeactivateMachine.CommandHandler, UpdateMachine.CommandHandler) CreateHandlers(
        IServiceProvider services)
    {
        var db = services.GetRequiredService<IAttendanceDbContext>();
        return (
            new GetMachineById.QueryHandler(db),
            new ActivateMachine.CommandHandler(db),
            new DeactivateMachine.CommandHandler(db),
            new UpdateMachine.CommandHandler(db, new UpdateMachine.Validator()));
    }

    private static async Task<AttendenceMachine> SeedMachineAsync(
        IAttendanceDbContext db,
        bool active = true)
    {
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1);
        if (!active)
        {
            machine.Deactivate();
        }

        db.Machines.Add(machine);
        await db.SaveChangesAsync();
        return machine;
    }

    [Fact]
    public async Task GetById_WhenMachineExists_ReturnsMachine()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var machine = await SeedMachineAsync(db);
        var (getById, _, _, _) = CreateHandlers(scope.ServiceProvider);

        var result = await getById.Handle(
            new GetMachineById.Query(machine.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(machine.Id, result.Value.MachineId);
        Assert.Equal(machine.MachineNumber, result.Value.MachineNumber);
        Assert.Equal(machine.IpAddress, result.Value.IpAddress);
        Assert.Equal(machine.Port, result.Value.Port);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task GetById_WhenMachineDoesNotExist_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var (getById, _, _, _) = CreateHandlers(scope.ServiceProvider);

        var result = await getById.Handle(
            new GetMachineById.Query(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendenceMachine.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetById_WhenIdIsEmpty_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var (getById, _, _, _) = CreateHandlers(scope.ServiceProvider);

        var result = await getById.Handle(
            new GetMachineById.Query(Guid.Empty),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendenceMachine.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Activate_WhenMachineInactive_ActivatesAndPersists()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var machine = await SeedMachineAsync(db, active: false);
        var (_, activate, _, _) = CreateHandlers(scope.ServiceProvider);

        var result = await activate.Handle(
            new ActivateMachine.Command(machine.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var saved = await db.Machines
            .AsNoTracking()
            .SingleAsync(m => m.Id == machine.Id);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Activate_WhenMachineDoesNotExist_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var (_, activate, _, _) = CreateHandlers(scope.ServiceProvider);

        var result = await activate.Handle(
            new ActivateMachine.Command(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendenceMachine.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Deactivate_WhenMachineActive_DeactivatesAndPersists()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var machine = await SeedMachineAsync(db);
        var (_, _, deactivate, _) = CreateHandlers(scope.ServiceProvider);

        var result = await deactivate.Handle(
            new DeactivateMachine.Command(machine.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var saved = await db.Machines
            .AsNoTracking()
            .SingleAsync(m => m.Id == machine.Id);
        Assert.False(saved.IsActive);
    }

    [Fact]
    public async Task Deactivate_WhenMachineDoesNotExist_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var (_, _, deactivate, _) = CreateHandlers(scope.ServiceProvider);

        var result = await deactivate.Handle(
            new DeactivateMachine.Command(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendenceMachine.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Update_WhenMachineExists_UpdatesIpAddressAndPortAndPersists()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var machine = await SeedMachineAsync(db);
        var (_, _, _, update) = CreateHandlers(scope.ServiceProvider);

        var result = await update.Handle(
            new UpdateMachine.Command(machine.Id, "192.168.3.210", 8080),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(machine.Id, result.Value.MachineId);

        var saved = await db.Machines
            .AsNoTracking()
            .SingleAsync(m => m.Id == machine.Id);
        Assert.Equal("192.168.3.210", saved.IpAddress);
        Assert.Equal(8080, saved.Port);
        Assert.Equal(machine.MachineNumber, saved.MachineNumber);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Update_WhenMachineDoesNotExist_ReturnsNotFound()
    {
        using var scope = CreateScope();
        var (_, _, _, update) = CreateHandlers(scope.ServiceProvider);

        var result = await update.Handle(
            new UpdateMachine.Command(Guid.NewGuid(), "192.168.3.210", 8080),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendenceMachine.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Update_WhenCommandIsInvalid_ThrowsValidationException()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var machine = await SeedMachineAsync(db);
        var (_, _, _, update) = CreateHandlers(scope.ServiceProvider);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            update.Handle(
                new UpdateMachine.Command(machine.Id, "", 0),
                CancellationToken.None));
    }
}