using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Modules.Attendence.Application.AttendenceRerords;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Attendence.Domain.Punches;
using Modules.Employees.Contracts;
using Modules.Shared.Results;
using ContractSchedule = Modules.Employees.Contracts.WorkSchedule;

namespace Application.IntegrationTests.Attendence;

public sealed class CreateAttendenceRecordCommandHandlerTests : AttendenceTestBase
{
    private const int EmployeeBadge = 100;
    private const string EmployeeId = "emp-1";

    public CreateAttendenceRecordCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    private (DateTime Today, DateTime ExpectedCheckIn, DateTime ExpectedCheckOut) Schedule
    {
        get
        {
            var today = DateTime.UtcNow.Date;
            return (today, today.AddHours(9), today.AddHours(17));
        }
    }

    private void SetupEmployeeFound()
    {
        var (_, expectedCheckIn, expectedCheckOut) = Schedule;
        EmployeeApi.Response = Result<EmployeeResponse>.Success(new EmployeeResponse(
            EmployeeId,
            EmployeeBadge,
            "John Doe",
            new ContractSchedule(
                StandardWorkTime: TimeSpan.FromHours(8),
                ExpectedCheckOutTime: expectedCheckOut,
                ExpectedCheckInTime: expectedCheckIn)));
    }

    private async Task SeedPunchAsync(
        IAttendanceDbContext db,
        DateTime punchAt)
    {
        db.Punches.Add(Punch.Create(MachineId, EmployeeBadge, punchAt, DateTime.UtcNow));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ReturnsFailure()
    {
        using var scope = CreateScope();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var command = new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, DateTime.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenNoOpenRecord_CreatesAndPersistsCheckInRecord()
    {
        SetupEmployeeFound();
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var (today, _, _) = Schedule;
        var punch = today.AddHours(9).AddMinutes(15);
        await SeedPunchAsync(db, punch);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var saved = await db.AttendanceRecords.SingleOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(EmployeeId, saved!.EmployeeId);
        Assert.Equal(punch, saved.CheckInAt);
        Assert.Null(saved.CheckOutAt);
        Assert.Equal(TimeSpan.FromMinutes(15), saved.LateTime);
    }

    [Fact]
    public async Task Handle_WhenOpenRecordExists_RegistersCheckOutAndComputesOvertime()
    {
        SetupEmployeeFound();
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var (today, _, _) = Schedule;
        await SeedPunchAsync(db, today.AddHours(8));
        var punch = today.AddHours(18);
        await SeedPunchAsync(db, punch);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(punch, record.CheckOutAt);
        Assert.Equal(TimeSpan.FromHours(10), record.WorkedTime);
        Assert.Equal(TimeSpan.FromHours(2), record.Overtime);
        Assert.Equal(TimeSpan.Zero, record.EarlyLeaveTime);
    }

    [Fact]
    public async Task Handle_WhenOpenRecordExists_ComputesEarlyLeave()
    {
        SetupEmployeeFound();
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var (today, _, _) = Schedule;
        await SeedPunchAsync(db, today.AddHours(8));
        var punch = today.AddHours(16);
        await SeedPunchAsync(db, punch);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(8), record.WorkedTime);
        Assert.Equal(TimeSpan.Zero, record.Overtime);
        Assert.Equal(TimeSpan.FromHours(1), record.EarlyLeaveTime);
    }

    [Fact]
    public async Task Handle_WhenLastRecordAlreadyCheckedOut_CreatesNewCheckInRecord()
    {
        SetupEmployeeFound();
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var (today, _, _) = Schedule;
        await SeedPunchAsync(db, today.AddHours(8));
        await SeedPunchAsync(db, today.AddHours(17));
        var punch = today.AddHours(20);
        await SeedPunchAsync(db, punch);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords
            .OrderByDescending(r => r.CreatedOnUtc)
            .ToListAsync();
        Assert.Equal(2, records.Count);
        Assert.NotNull(records[0].CheckInAt);
        Assert.Null(records[0].CheckOutAt);
    }
}