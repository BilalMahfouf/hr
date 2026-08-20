using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Machines;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Infrastructure.Presistance;

namespace Application.Tests.Attendence;

public sealed class UpdateMachineTests
{
    private static (UpdateMachine.CommandHandler handler, AttendanceDbContext db) Arrange()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        var handler = new UpdateMachine.CommandHandler(
            db,
            new UpdateMachine.Validator());

        return (handler, db);
    }

    [Fact]
    public async Task Handle_WhenMachineExists_UpdatesIpAddressAndPort()
    {
        var (handler, db) = Arrange();
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1);
        db.Machines.Add(machine);
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new UpdateMachine.Command(machine.Id, "192.168.3.210", 8080));

        Assert.True(result.IsSuccess);
        Assert.Equal(machine.Id, result.Value.MachineId);

        Assert.Equal("192.168.3.210", machine.IpAddress);
        Assert.Equal(8080, machine.Port);
        Assert.Equal(1, machine.MachineNumber);
        Assert.True(machine.IsActive);
    }

    [Fact]
    public async Task Handle_WhenMachineDoesNotExist_ReturnsNotFound()
    {
        var (handler, db) = Arrange();

        var result = await handler.Handle(
            new UpdateMachine.Command(Guid.NewGuid(), "192.168.3.210", 8080));

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendenceMachine.NotFound", result.Error.Code);
        Assert.Empty(db.Machines);
    }

    [Theory]
    [InlineData("", 8080)]
    [InlineData("192.168.3.210", 0)]
    [InlineData("192.168.3.210", -1)]
    public async Task Handle_WithInvalidCommand_ThrowsValidationException(
        string ipAddress,
        int port)
    {
        var (handler, db) = Arrange();
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1);
        db.Machines.Add(machine);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(
                new UpdateMachine.Command(machine.Id, ipAddress, port)));

        Assert.Equal("192.168.3.205", machine.IpAddress);
        Assert.Equal(4370, machine.Port);
    }

    [Fact]
    public async Task Handle_WhenIdIsEmpty_ThrowsValidationException()
    {
        var (handler, db) = Arrange();

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(
                new UpdateMachine.Command(Guid.Empty, "192.168.3.210", 8080)));

        Assert.Empty(db.Machines);
    }
}