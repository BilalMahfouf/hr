using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Machines;
using Modules.Attendence.Infrastructure.Presistance;

namespace Application.Tests.Attendence;

public sealed class CreateMachineTests
{
    private static (CreateMachine.CommandHandler handler, AttendanceDbContext db) Arrange()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        var handler = new CreateMachine.CommandHandler(
            db,
            new CreateMachine.Validator());

        return (handler, db);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesMachineWithDefaults()
    {
        var (handler, db) = Arrange();

        var result = await handler.Handle(
            new CreateMachine.Command("192.168.3.205", 1, null));

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.MachineId);

        var machine = Assert.Single(db.Machines);
        Assert.Equal("192.168.3.205", machine.IpAddress);
        Assert.Equal(1, machine.MachineNumber);
        Assert.Equal(4370, machine.Port);
        Assert.True(machine.IsActive);
    }

    [Fact]
    public async Task Handle_WithPort_UsesProvidedPort()
    {
        var (handler, db) = Arrange();

        var result = await handler.Handle(
            new CreateMachine.Command("192.168.3.206", 2, 8080));

        Assert.True(result.IsSuccess);
        var machine = Assert.Single(db.Machines);
        Assert.Equal(8080, machine.Port);
    }

    [Theory]
    [InlineData("", 1, null)]
    [InlineData("192.168.3.205", 0, null)]
    [InlineData("192.168.3.205", -1, null)]
    [InlineData("192.168.3.205", 1, 0)]
    [InlineData("192.168.3.205", 1, -1)]
    public async Task Handle_WithInvalidCommand_ThrowsValidationException(
        string ipAddress,
        int machineNumber,
        int? port)
    {
        var (handler, db) = Arrange();

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(
                new CreateMachine.Command(ipAddress, machineNumber, port)));

        Assert.Empty(db.Machines);
    }
}