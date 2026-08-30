using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modules.Attendence.Application.Abstractions;
using Modules.Attendence.Application.Importer;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Domain.Punches;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Shared.Infrastructure.Outbox;
using Moq;
using PublicApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Attendence;

public sealed class MachinesTests : AttendenceTestBase
{
    private readonly Mock<IAttendanceMachineReaderFactory> _factoryMock = new();

    public MachinesTests(PostgresFixture fixture) : base(fixture)
    {
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton<IAttendanceMachineReaderFactory>(_factoryMock.Object);
        services.AddSingleton<IValidator<ImportAttendanceLogs.Command>>(
            new ImportAttendanceLogs.Validator());
    }

    private static ImportAttendanceLogs.CommandHandler CreateHandler(
        IServiceProvider services,
        IAttendanceDbContext db)
    {
        return new ImportAttendanceLogs.CommandHandler(
            db,
            services.GetRequiredService<IAttendanceMachineReaderFactory>(),
            services.GetRequiredService<IValidator<ImportAttendanceLogs.Command>>(),
            services.GetRequiredService<ILogger<ImportAttendanceLogs.CommandHandler>>());
    }

    private void SetupReader(AttendenceMachine machine, IReadOnlyList<RawAttendanceLog> logs)
    {
        var reader = new Mock<IAttendanceMachineReader>();
        reader
            .Setup(r => r.GetLogsAsync(
                It.IsAny<AttendenceMachine>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        _factoryMock
            .Setup(f => f.Create(It.Is<AttendenceMachine>(m => m.MachineNumber == machine.MachineNumber)))
            .Returns(reader.Object);
    }

    [Fact]
    public async Task Machine_Persists()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();

        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);
        db.Machines.Add(machine);
        await db.SaveChangesAsync();

        var saved = await db.Machines.SingleAsync();
        Assert.Equal("192.168.3.205", saved.IpAddress);
        Assert.Equal(1, saved.MachineNumber);
        Assert.Equal(4370, saved.Port);
        Assert.Equal(MachineType.ZKTecoGateway, saved.Type);
        Assert.True(saved.IsActive);
    }

    [Fact]
    public async Task Import_PersistsPunchesAndWritesPunchCreatedEventToOutbox()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();

        var machine = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);
        db.Machines.Add(machine);
        await db.SaveChangesAsync();

        var date = new DateOnly(2026, 8, 17);
        var timestamp = new DateTime(2026, 8, 17, 9, 0, 0);

        SetupReader(machine, new List<RawAttendanceLog>
        {
            new(machine.Id, "100", timestamp, 1, 0, 0, "SN-1", 1)
        });

        var handler = CreateHandler(scope.ServiceProvider, db);
        var result = await handler.Handle(
            new ImportAttendanceLogs.Command(date, date),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.PunchCount);

        var punch = await db.Punches.SingleAsync();
        Assert.Equal(machine.Id, punch.MachineId);
        Assert.Equal(100, punch.EmployeeBadge);
        Assert.Equal(timestamp, punch.PunchOccurredAt);

        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var message = await appDb.Set<OutboxMessage>()
            .OrderByDescending(m => m.CreatedOnUtc)
            .FirstOrDefaultAsync(m => m.Name.Contains(nameof(PunchCreatedDomainEvent)));

        Assert.NotNull(message);
        Assert.Equal(
            typeof(PunchCreatedDomainEvent).AssemblyQualifiedName,
            message!.Name);
        Assert.Null(message.ProcessedOnUtc);
    }

    [Fact]
    public async Task Import_WhenMachineReadFails_ContinuesWithOtherMachines()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();

        var machine1 = AttendenceMachine.Create(MachineId.New(), "192.168.3.205", 1, MachineType.ZKTecoGateway);
        var machine2 = AttendenceMachine.Create(MachineId.New(), "192.168.3.206", 2, MachineType.ZKTecoGateway);
        db.Machines.AddRange(machine1, machine2);
        await db.SaveChangesAsync();

        var date = new DateOnly(2026, 8, 17);

        var failingReader = new Mock<IAttendanceMachineReader>();
        failingReader
            .Setup(r => r.GetLogsAsync(
                It.IsAny<AttendenceMachine>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection failed"));
        _factoryMock
            .Setup(f => f.Create(It.Is<AttendenceMachine>(m => m.MachineNumber == 1)))
            .Returns(failingReader.Object);

        SetupReader(machine2, new List<RawAttendanceLog>
        {
            new(machine2.Id, "100", new DateTime(2026, 8, 17, 9, 0, 0), 1, 0, 0, "SN-2", 2)
        });

        var handler = CreateHandler(scope.ServiceProvider, db);
        var result = await handler.Handle(
            new ImportAttendanceLogs.Command(date, date),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.MachineCount);
        Assert.Equal(1, result.Value.PunchCount);

        var punches = await db.Punches.ToListAsync();
        var punch = Assert.Single(punches);
        Assert.Equal(machine2.Id, punch.MachineId);
    }
}