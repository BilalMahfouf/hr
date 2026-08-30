using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Machines;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Infrastructure.Presistance;

namespace Application.Tests.Attendence;

public sealed class GetAllMachinesTests
{
    private static (GetAllMachines.QueryHandler handler, AttendanceDbContext db) Arrange()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        return (new GetAllMachines.QueryHandler(db), db);
    }

    [Fact]
    public async Task Handle_ReturnsAllMachinesMapped()
    {
        var (handler, db) = Arrange();
        var machine1 = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);
        var machine2 = AttendenceMachine.Create(MachineId.New(), "192.168.3.206", 2, MachineType.ZKTecoSdk, 8080);
        machine2.Deactivate();
        db.Machines.AddRange(machine1, machine2);
        await db.SaveChangesAsync();

        var result = await handler.Handle(new GetAllMachines.Query());

        Assert.True(result.IsSuccess);
        var machines = result.Value.ToList();
        Assert.Equal(2, machines.Count);
        Assert.Contains(machines, m =>
            m.MachineId == machine1.Id &&
            m.MachineNumber == 1 &&
            m.IpAddress == "192.168.3.205" &&
            m.Port == 4370 &&
            m.Type == MachineType.ZKTecoGateway &&
            m.IsActive &&
            m.CreatedOnUtc == machine1.CreatedOnUtc);
        Assert.Contains(machines, m =>
            m.MachineId == machine2.Id &&
            m.MachineNumber == 2 &&
            m.IpAddress == "192.168.3.206" &&
            m.Port == 8080 &&
            m.Type == MachineType.ZKTecoSdk &&
            !m.IsActive &&
            m.CreatedOnUtc == machine2.CreatedOnUtc);
    }

    [Fact]
    public async Task Handle_WhenNoMachines_ReturnsEmptyList()
    {
        var (handler, _) = Arrange();

        var result = await handler.Handle(new GetAllMachines.Query());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}