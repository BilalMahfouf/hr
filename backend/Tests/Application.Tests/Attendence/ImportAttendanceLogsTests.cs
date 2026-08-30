using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Application.Importer;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Domain.Punches;
using Modules.Attendence.Infrastructure.Presistance;
using Moq;

namespace Application.Tests.Attendence;

public sealed class ImportAttendanceLogsTests
{
    private static readonly DateOnly From = new(2026, 8, 17);
    private static readonly DateOnly To = new(2026, 8, 17);

    private static RawAttendanceLog Log(
        AttendenceMachine machine,
        string enroll,
        DateTime timestamp,
        int machineNumber)
        => new(
            machine.Id,
            enroll,
            timestamp,
            VerifyMode: 1,
            InOutMode: 0,
            WorkCode: 0,
            DeviceSerialNumber: "SN-1",
            machineNumber);

    private static (ImportAttendanceLogs.CommandHandler handler, AttendanceDbContext db, Mock<IAttendanceMachineReaderFactory> factoryMock) Arrange(
        params AttendenceMachine[] machines)
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        db.Machines.AddRange(machines);
        db.SaveChanges();

        var factoryMock = new Mock<IAttendanceMachineReaderFactory>();

        var handler = new ImportAttendanceLogs.CommandHandler(
            db,
            factoryMock.Object,
            new ImportAttendanceLogs.Validator(),
            NullLogger<ImportAttendanceLogs.CommandHandler>.Instance);

        return (handler, db, factoryMock);
    }

    private static void SetupReader(
        Mock<IAttendanceMachineReaderFactory> factoryMock,
        AttendenceMachine machine,
        IReadOnlyList<RawAttendanceLog> logs)
    {
        var reader = new Mock<IAttendanceMachineReader>();
        reader
            .Setup(r => r.GetLogsAsync(
                It.Is<AttendenceMachine>(m => m.MachineNumber == machine.MachineNumber),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        factoryMock
            .Setup(f => f.Create(It.Is<AttendenceMachine>(m => m.MachineNumber == machine.MachineNumber)))
            .Returns(reader.Object);
    }

    [Fact]
    public async Task Handle_ReadsAllActiveMachinesAndPersistsPunches()
    {
        var machine1 = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);
        var machine2 = AttendenceMachine.Create(MachineId.New(), "192.168.3.206", 2, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock) = Arrange(machine1, machine2);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        SetupReader(factoryMock, machine1, new List<RawAttendanceLog> { Log(machine1, "100", ts, 1) });
        SetupReader(factoryMock, machine2, new List<RawAttendanceLog> { Log(machine2, "101", ts, 2) });

        var result = await handler.Handle(new ImportAttendanceLogs.Command(From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.MachineCount);
        Assert.Equal(2, result.Value.PunchCount);
        Assert.Equal(2, await db.Punches.CountAsync());
        Assert.Contains(db.Punches, p => p.EmployeeBadge == 100 && p.MachineId == machine1.Id);
        Assert.Contains(db.Punches, p => p.EmployeeBadge == 101 && p.MachineId == machine2.Id);
    }

    [Fact]
    public async Task Handle_SkipsInactiveMachines()
    {
        var active = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);
        var inactive = AttendenceMachine.Create(MachineId.New(), "192.168.3.206", 2, MachineType.ZKTecoGateway);
        inactive.Deactivate();

        var (handler, db, factoryMock) = Arrange(active, inactive);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        SetupReader(factoryMock, active, new List<RawAttendanceLog> { Log(active, "100", ts, 1) });

        var result = await handler.Handle(new ImportAttendanceLogs.Command(From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.PunchCount);
        Assert.Single(await db.Punches.ToListAsync());
        factoryMock.Verify(
            f => f.Create(It.Is<AttendenceMachine>(m => m.MachineNumber == 2)),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMachineReadFails_ContinuesWithOtherMachines()
    {
        var machine1 = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);
        var machine2 = AttendenceMachine.Create(MachineId.New(), "192.168.3.206", 2, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock) = Arrange(machine1, machine2);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);

        var failingReader = new Mock<IAttendanceMachineReader>();
        failingReader
            .Setup(r => r.GetLogsAsync(
                It.IsAny<AttendenceMachine>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection failed"));
        factoryMock
            .Setup(f => f.Create(It.Is<AttendenceMachine>(m => m.MachineNumber == 1)))
            .Returns(failingReader.Object);

        SetupReader(factoryMock, machine2, new List<RawAttendanceLog> { Log(machine2, "100", ts, 2) });

        var result = await handler.Handle(new ImportAttendanceLogs.Command(From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.MachineCount);
        Assert.Equal(1, result.Value.PunchCount);
        var punch = Assert.Single(await db.Punches.ToListAsync());
        Assert.Equal(machine2.Id, punch.MachineId);
    }

    [Fact]
    public async Task Handle_SkipsLogWithInvalidEmployeeNumber()
    {
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock) = Arrange(machine);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        SetupReader(factoryMock, machine, new List<RawAttendanceLog> { Log(machine, "not-a-number", ts, 1) });

        var result = await handler.Handle(new ImportAttendanceLogs.Command(From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.PunchCount);
        Assert.Empty(await db.Punches.ToListAsync());
    }

    [Fact]
    public async Task Handle_DeduplicatesExistingPunches()
    {
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock) = Arrange(machine);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        db.Punches.Add(Punch.Create(machine.Id, 100, ts, DateTime.UtcNow));
        db.SaveChanges();

        SetupReader(factoryMock, machine, new List<RawAttendanceLog> { Log(machine, "100", ts, 1) });

        var result = await handler.Handle(new ImportAttendanceLogs.Command(From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.PunchCount);
        Assert.Single(await db.Punches.ToListAsync());
    }

    [Fact]
    public async Task Handle_WhenFromAfterTo_ThrowsValidationException()
    {
        var (handler, _, _) = Arrange();

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new ImportAttendanceLogs.Command(
                new DateOnly(2026, 8, 17),
                new DateOnly(2026, 8, 16))));
    }
}
