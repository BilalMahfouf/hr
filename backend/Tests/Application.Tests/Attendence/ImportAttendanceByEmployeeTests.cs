using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Application.Importer;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Domain.Punches;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Employees.Contracts;
using Modules.Shared.Results;
using Moq;

namespace Application.Tests.Attendence;

public sealed class ImportAttendanceByEmployeeTests
{
    private static readonly DateOnly From = new(2026, 8, 17);
    private static readonly DateOnly To = new(2026, 8, 17);
    private const string EmployeeId = "emp-1";
    private const int EmployeeBadge = 100;

    private static RawAttendanceLog Log(
        AttendenceMachine machine,
        string enroll,
        DateTime timestamp)
        => new(
            machine.Id,
            enroll,
            timestamp,
            VerifyMode: 1,
            InOutMode: 0,
            WorkCode: 0,
            DeviceSerialNumber: "SN-1",
            machine.MachineNumber);

    private static EmployeeResponse Employee(string id, int badge) =>
        new(id, badge, "John Doe", DummySchedule());

    private static WorkScheduleReadDto DummySchedule() => new(
        Guid.NewGuid(), Guid.NewGuid(),
        new TimeOnly(9, 0), new TimeOnly(17, 0),
        TimeSpan.FromHours(8), 0,
        new TimeOnly(12, 0), new TimeOnly(13, 0),
        5, 5, true,
        DateTime.UtcNow.Date.AddHours(9),
        DateTime.UtcNow.Date.AddHours(17),
        DateTime.UtcNow.Date.AddHours(12),
        DateTime.UtcNow.Date.AddHours(13),
        EmployeeWorkStatus.Work);

    // ──────────────────────────────────────────────
    //  Import by Employee — Arrange helpers
    // ──────────────────────────────────────────────

    private static (
        ImportAttendanceByEmployee.ImportByEmployeeHandler handler,
        AttendanceDbContext db,
        Mock<IAttendanceMachineReaderFactory> factoryMock,
        Mock<IEmployeeApi> employeeApi)
        ArrangeEmployee(params AttendenceMachine[] machines)
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        db.Machines.AddRange(machines);
        db.SaveChanges();

        var factoryMock = new Mock<IAttendanceMachineReaderFactory>();
        var employeeApi = new Mock<IEmployeeApi>();

        var handler = new ImportAttendanceByEmployee.ImportByEmployeeHandler(
            db,
            factoryMock.Object,
            employeeApi.Object,
            new ImportAttendanceByEmployee.ImportByEmployeeValidator(),
            NullLogger<ImportAttendanceByEmployee.ImportByEmployeeHandler>.Instance);

        return (handler, db, factoryMock, employeeApi);
    }

    private static void SetupEmployeeFound(
        Mock<IEmployeeApi> employeeApi,
        string id,
        int badge)
    {
        employeeApi
            .Setup(x => x.GetEmployeeByIdAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EmployeeResponse>.Success(Employee(id, badge)));
    }

    private static void SetupEmployeeNotFound(
        Mock<IEmployeeApi> employeeApi)
    {
        employeeApi
            .Setup(x => x.GetEmployeeByIdAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EmployeeResponse>.Failure(EmployeeErrors.NotFound));
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
            .Setup(f => f.Create(
                It.Is<AttendenceMachine>(m => m.MachineNumber == machine.MachineNumber)))
            .Returns(reader.Object);
    }

    // ──────────────────────────────────────────────
    //  Import by Employee — Tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ImportByEmployee_WhenEmployeeNotFound_ReturnsFailure()
    {
        var machine = AttendenceMachine.Create(
            MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);

        var (handler, _, _, employeeApi) = ArrangeEmployee(machine);
        SetupEmployeeNotFound(employeeApi);

        var result = await handler.Handle(
            new ImportAttendanceByEmployee.ImportByEmployeeCommand(EmployeeId, From, To));

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task ImportByEmployee_WhenEmployeeFound_ImportsMatchingPunches()
    {
        var machine = AttendenceMachine.Create(
            MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock, employeeApi) = ArrangeEmployee(machine);
        SetupEmployeeFound(employeeApi, EmployeeId, EmployeeBadge);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        SetupReader(factoryMock, machine, new List<RawAttendanceLog>
        {
            Log(machine, "100", ts),
            Log(machine, "101", ts.AddHours(1)),
            Log(machine, "100", ts.AddHours(2))
        });

        var result = await handler.Handle(
            new ImportAttendanceByEmployee.ImportByEmployeeCommand(EmployeeId, From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.MachineCount);
        Assert.Equal(2, result.Value.PunchCount);
        Assert.Equal(2, await db.Punches.CountAsync());
        Assert.All(db.Punches, p => Assert.Equal(EmployeeBadge, p.EmployeeBadge));
    }

    [Fact]
    public async Task ImportByEmployee_FiltersOnlyMatchingBadge()
    {
        var machine = AttendenceMachine.Create(
            MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock, employeeApi) = ArrangeEmployee(machine);
        SetupEmployeeFound(employeeApi, EmployeeId, EmployeeBadge);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        SetupReader(factoryMock, machine, new List<RawAttendanceLog>
        {
            Log(machine, "200", ts),
            Log(machine, "201", ts.AddHours(1))
        });

        var result = await handler.Handle(
            new ImportAttendanceByEmployee.ImportByEmployeeCommand(EmployeeId, From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.PunchCount);
        Assert.Empty(await db.Punches.ToListAsync());
    }

    [Fact]
    public async Task ImportByEmployee_OnlyCountsMachinesWithPunches()
    {
        var machine1 = AttendenceMachine.Create(
            MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);
        var machine2 = AttendenceMachine.Create(
            MachineId.New(), "192.168.3.206", 2, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock, employeeApi) = ArrangeEmployee(machine1, machine2);
        SetupEmployeeFound(employeeApi, EmployeeId, EmployeeBadge);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        SetupReader(factoryMock, machine1, new List<RawAttendanceLog>
        {
            Log(machine1, "100", ts)
        });
        SetupReader(factoryMock, machine2, new List<RawAttendanceLog>
        {
            Log(machine2, "200", ts),
            Log(machine2, "201", ts)
        });

        var result = await handler.Handle(
            new ImportAttendanceByEmployee.ImportByEmployeeCommand(EmployeeId, From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.MachineCount);
        Assert.Equal(1, result.Value.PunchCount);
        Assert.Single(await db.Punches.ToListAsync());
    }

    [Fact]
    public async Task ImportByEmployee_DeduplicatesExistingPunches()
    {
        var machine = AttendenceMachine.Create(
            MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock, employeeApi) = ArrangeEmployee(machine);
        SetupEmployeeFound(employeeApi, EmployeeId, EmployeeBadge);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        db.Punches.Add(Punch.Create(machine.Id, EmployeeBadge, ts, DateTime.UtcNow));
        db.SaveChanges();

        SetupReader(factoryMock, machine, new List<RawAttendanceLog>
        {
            Log(machine, "100", ts)
        });

        var result = await handler.Handle(
            new ImportAttendanceByEmployee.ImportByEmployeeCommand(EmployeeId, From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.PunchCount);
        Assert.Single(await db.Punches.ToListAsync());
    }

    [Fact]
    public async Task ImportByEmployee_WhenMachineReadFails_ContinuesWithOtherMachines()
    {
        var machine1 = AttendenceMachine.Create(
            MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);
        var machine2 = AttendenceMachine.Create(
            MachineId.New(), "192.168.3.206", 2, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock, employeeApi) = ArrangeEmployee(machine1, machine2);
        SetupEmployeeFound(employeeApi, EmployeeId, EmployeeBadge);

        var failingReader = new Mock<IAttendanceMachineReader>();
        failingReader
            .Setup(r => r.GetLogsAsync(
                It.IsAny<AttendenceMachine>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection failed"));
        factoryMock
            .Setup(f => f.Create(
                It.Is<AttendenceMachine>(m => m.MachineNumber == 1)))
            .Returns(failingReader.Object);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        SetupReader(factoryMock, machine2, new List<RawAttendanceLog>
        {
            Log(machine2, "100", ts)
        });

        var result = await handler.Handle(
            new ImportAttendanceByEmployee.ImportByEmployeeCommand(EmployeeId, From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.MachineCount);
        Assert.Equal(1, result.Value.PunchCount);
        var punch = Assert.Single(await db.Punches.ToListAsync());
        Assert.Equal(machine2.Id, punch.MachineId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ImportByEmployee_WithEmptyEmployeeId_ThrowsValidationException(
        string? employeeId)
    {
        var (handler, _, _, _) = ArrangeEmployee();

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(
                new ImportAttendanceByEmployee.ImportByEmployeeCommand(employeeId!, From, To)));
    }

    [Fact]
    public async Task ImportByEmployee_WhenFromAfterTo_ThrowsValidationException()
    {
        var (handler, _, _, _) = ArrangeEmployee();

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new ImportAttendanceByEmployee.ImportByEmployeeCommand(
                EmployeeId,
                new DateOnly(2026, 8, 17),
                new DateOnly(2026, 8, 16))));
    }

    // ──────────────────────────────────────────────
    //  Import by Machine — Arrange helpers
    // ──────────────────────────────────────────────

    private static (
        ImportAttendanceByEmployee.ImportByMachineHandler handler,
        AttendanceDbContext db,
        Mock<IAttendanceMachineReaderFactory> factoryMock)
        ArrangeMachine(AttendenceMachine? machine = null)
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        if (machine is not null)
        {
            db.Machines.Add(machine);
            db.SaveChanges();
        }

        var factoryMock = new Mock<IAttendanceMachineReaderFactory>();

        var handler = new ImportAttendanceByEmployee.ImportByMachineHandler(
            db,
            factoryMock.Object,
            new ImportAttendanceByEmployee.ImportByMachineValidator(),
            NullLogger<ImportAttendanceByEmployee.ImportByMachineHandler>.Instance);

        return (handler, db, factoryMock);
    }

    private static void SetupReaderForMachine(
        Mock<IAttendanceMachineReaderFactory> factoryMock,
        AttendenceMachine machine,
        IReadOnlyList<RawAttendanceLog> logs)
    {
        var reader = new Mock<IAttendanceMachineReader>();
        reader
            .Setup(r => r.GetLogsAsync(
                It.Is<AttendenceMachine>(m => m.Id == machine.Id),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        factoryMock
            .Setup(f => f.Create(
                It.Is<AttendenceMachine>(m => m.Id == machine.Id)))
            .Returns(reader.Object);
    }

    // ──────────────────────────────────────────────
    //  Import by Machine — Tests
    // ──────────────────────────────────────────────

    [Fact]
    public async Task ImportByMachine_WhenMachineNotFound_ReturnsFailure()
    {
        var (handler, _, _) = ArrangeMachine();

        var result = await handler.Handle(
            new ImportAttendanceByEmployee.ImportByMachineCommand(Guid.NewGuid(), From, To));

        Assert.False(result.IsSuccess);
        Assert.Equal(
            MachineErrors.MachineNotFound(Guid.Empty).Code.Split('.')[0],
            result.Error.Code.Split('.')[0]);
    }

    [Fact]
    public async Task ImportByMachine_WhenMachineFound_ImportsAllPunches()
    {
        var machine = AttendenceMachine.Create(
            MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock) = ArrangeMachine(machine);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        SetupReaderForMachine(factoryMock, machine, new List<RawAttendanceLog>
        {
            Log(machine, "100", ts),
            Log(machine, "101", ts.AddHours(1))
        });

        var result = await handler.Handle(
            new ImportAttendanceByEmployee.ImportByMachineCommand(machine.Id, From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.MachineCount);
        Assert.Equal(2, result.Value.PunchCount);
        Assert.Equal(2, await db.Punches.CountAsync());
    }

    [Fact]
    public async Task ImportByMachine_DeduplicatesExistingPunches()
    {
        var machine = AttendenceMachine.Create(
            MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock) = ArrangeMachine(machine);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        db.Punches.Add(Punch.Create(machine.Id, 100, ts, DateTime.UtcNow));
        db.SaveChanges();

        SetupReaderForMachine(factoryMock, machine, new List<RawAttendanceLog>
        {
            Log(machine, "100", ts)
        });

        var result = await handler.Handle(
            new ImportAttendanceByEmployee.ImportByMachineCommand(machine.Id, From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.PunchCount);
        Assert.Single(await db.Punches.ToListAsync());
    }

    [Fact]
    public async Task ImportByMachine_WhenMachineReadFails_ReturnsZeroCounts()
    {
        var machine = AttendenceMachine.Create(
            MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock) = ArrangeMachine(machine);

        var failingReader = new Mock<IAttendanceMachineReader>();
        failingReader
            .Setup(r => r.GetLogsAsync(
                It.IsAny<AttendenceMachine>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection failed"));
        factoryMock
            .Setup(f => f.Create(
                It.Is<AttendenceMachine>(m => m.Id == machine.Id)))
            .Returns(failingReader.Object);

        var result = await handler.Handle(
            new ImportAttendanceByEmployee.ImportByMachineCommand(machine.Id, From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.MachineCount);
        Assert.Equal(0, result.Value.PunchCount);
        Assert.Empty(await db.Punches.ToListAsync());
    }

    [Fact]
    public async Task ImportByMachine_SkipsInvalidEmployeeNumbers()
    {
        var machine = AttendenceMachine.Create(
            MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);

        var (handler, db, factoryMock) = ArrangeMachine(machine);

        var ts = new DateTime(2026, 8, 17, 9, 0, 0);
        SetupReaderForMachine(factoryMock, machine, new List<RawAttendanceLog>
        {
            Log(machine, "not-a-number", ts),
            Log(machine, "0", ts),
            Log(machine, "100", ts)
        });

        var result = await handler.Handle(
            new ImportAttendanceByEmployee.ImportByMachineCommand(machine.Id, From, To));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.PunchCount);
        var punch = Assert.Single(await db.Punches.ToListAsync());
        Assert.Equal(100, punch.EmployeeBadge);
    }

    [Fact]
    public async Task ImportByMachine_WhenFromAfterTo_ThrowsValidationException()
    {
        var (handler, _, _) = ArrangeMachine();

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new ImportAttendanceByEmployee.ImportByMachineCommand(
                Guid.NewGuid(),
                new DateOnly(2026, 8, 17),
                new DateOnly(2026, 8, 16))));
    }

    [Fact]
    public async Task ImportByMachine_WithEmptyMachineId_ThrowsValidationException()
    {
        var (handler, _, _) = ArrangeMachine();

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new ImportAttendanceByEmployee.ImportByMachineCommand(
                Guid.Empty, From, To)));
    }
}
