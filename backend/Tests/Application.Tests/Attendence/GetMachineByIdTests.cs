using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Machines;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Infrastructure.Presistance;

namespace Application.Tests.Attendence;

public sealed class GetMachineByIdTests
{
    private static (GetMachineById.QueryHandler handler, AttendanceDbContext db) Arrange()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        return (new GetMachineById.QueryHandler(db), db);
    }

    [Fact]
    public async Task Handle_WhenMachineExists_ReturnsMappedResponse()
    {
        var (handler, db) = Arrange();
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, 8080);
        db.Machines.Add(machine);
        await db.SaveChangesAsync();

        var result = await handler.Handle(
            new GetMachineById.Query(machine.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(machine.Id, result.Value.MachineId);
        Assert.Equal(1, result.Value.MachineNumber);
        Assert.Equal("192.168.3.205", result.Value.IpAddress);
        Assert.Equal(8080, result.Value.Port);
        Assert.True(result.Value.IsActive);
        Assert.Equal(machine.CreatedOnUtc, result.Value.CreatedOnUtc);
    }

    [Fact]
    public async Task Handle_WhenMachineDoesNotExist_ReturnsNotFound()
    {
        var (handler, _) = Arrange();

        var result = await handler.Handle(
            new GetMachineById.Query(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendenceMachine.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenIdIsEmpty_ReturnsNotFound()
    {
        var (handler, _) = Arrange();

        var result = await handler.Handle(
            new GetMachineById.Query(Guid.Empty));

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendenceMachine.NotFound", result.Error.Code);
    }
}