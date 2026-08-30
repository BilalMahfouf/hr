using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Machines;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Infrastructure.Presistance;

namespace Application.Tests.Attendence;

public sealed class ActivateMachineTests
{
    private static (ActivateMachine.CommandHandler handler, AttendanceDbContext db) Arrange()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        return (new ActivateMachine.CommandHandler(db), db);
    }

    [Fact]
    public async Task Handle_WhenMachineExistsAndInactive_ActivatesMachine()
    {
        var (handler, db) = Arrange();
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);
        machine.Deactivate();
        db.Machines.Add(machine);
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new ActivateMachine.Command(machine.Id));

        Assert.True(result.IsSuccess);
        Assert.True(machine.IsActive);
        Assert.True(db.Machines.Single().IsActive);
    }

    [Fact]
    public async Task Handle_WhenMachineAlreadyActive_IsIdempotent()
    {
        var (handler, db) = Arrange();
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);
        db.Machines.Add(machine);
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new ActivateMachine.Command(machine.Id));

        Assert.True(result.IsSuccess);
        Assert.True(machine.IsActive);
        Assert.True(db.Machines.Single().IsActive);
    }

    [Fact]
    public async Task Handle_WhenMachineDoesNotExist_ReturnsNotFound()
    {
        var (handler, _) = Arrange();

        var result = await handler.Handle(
            new ActivateMachine.Command(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendenceMachine.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenIdIsEmpty_ReturnsNotFound()
    {
        var (handler, _) = Arrange();

        var result = await handler.Handle(
            new ActivateMachine.Command(Guid.Empty));

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendenceMachine.NotFound", result.Error.Code);
    }
}