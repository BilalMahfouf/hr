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

    private static (ImportAttendanceLogs.CommandHandler handler, AttendanceDbContext db, Mock<IAttendanceMachineReader> reader) Arrange(
        params AttendenceMachine[] machines)
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        db.Machines.AddRange(machines);
        db.SaveChanges();

        var reader = new Mock<IAttendanceMachineReader>();

        var handler = new ImportAttendanceLogs.CommandHandler(
            db,
            reader.Object,
            new ImportAttendanceLogs.Validator(),
            NullLogger<ImportAttendanceLogs.CommandHandler>.Instance);

        return (handler, db, reader);
    }

    [Fact]
    public async Task Handle_ReadsAllActiveMachinesAndPersistsPunches()
    {
        var machine1 = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1);
        var machine2 = AttendenceMachine.Create(MachineId.New(), "192.168.3.206", 2);

        var (handler, db, reader) = Arrange(machine1, machine2);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        reader
            .Setup(r => r.GetLogsAsync(
                It.Is<AttendenceMachine>(m => m.MachineNumber == 1),
                From, To, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawAttendanceLog> { Log(machine1, "100", ts, 1) });
        reader
            .Setup(r => r.GetLogsAsync(
                It.Is<AttendenceMachine>(m => m.MachineNumber == 2),
                From, To, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawAttendanceLog> { Log(machine2, "101", ts, 2) });

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
        var active = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1);
        var inactive = AttendenceMachine.Create(MachineId.New(), "192.168.3.206", 2);
        inactive.Deactivate();

        var (handler, db, reader) = Arrange(active, inactive);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        reader
            .Setup(r => r.GetLogsAsync(
                It.IsAny<AttendenceMachine>(),
                From, To, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawAttendanceLog> { Log(active, "100", ts, 1) });

        var result = await handler.Handle(new ImportAttendanceLogs.Command(From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.PunchCount);
        Assert.Single(await db.Punches.ToListAsync());
        reader.Verify(
            r => r.GetLogsAsync(
                It.Is<AttendenceMachine>(m => m.MachineNumber == 2),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMachineReadFails_ContinuesWithOtherMachines()
    {
        var machine1 = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1);
        var machine2 = AttendenceMachine.Create(MachineId.New(), "192.168.3.206", 2);

        var (handler, db, reader) = Arrange(machine1, machine2);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        reader
            .Setup(r => r.GetLogsAsync(
                It.Is<AttendenceMachine>(m => m.MachineNumber == 1),
                From, To, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection failed"));
        reader
            .Setup(r => r.GetLogsAsync(
                It.Is<AttendenceMachine>(m => m.MachineNumber == 2),
                From, To, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawAttendanceLog> { Log(machine2, "100", ts, 2) });

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
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1);

        var (handler, db, reader) = Arrange(machine);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        reader
            .Setup(r => r.GetLogsAsync(
                It.IsAny<AttendenceMachine>(),
                From, To, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawAttendanceLog> { Log(machine, "not-a-number", ts, 1) });

        var result = await handler.Handle(new ImportAttendanceLogs.Command(From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.PunchCount);
        Assert.Empty(await db.Punches.ToListAsync());
    }

    [Fact]
    public async Task Handle_DeduplicatesExistingPunches()
    {
        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1);

        var (handler, db, reader) = Arrange(machine);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        db.Punches.Add(Punch.Create(machine.Id, 100, ts, DateTime.UtcNow));
        db.SaveChanges();

        reader
            .Setup(r => r.GetLogsAsync(
                It.IsAny<AttendenceMachine>(),
                From, To, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RawAttendanceLog> { Log(machine, "100", ts, 1) });

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