using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modules.Attendence.Application.AttendenceRerords;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Attendence.Domain.Punches;
using Modules.Employees.Contracts;
using Modules.Shared.Results;

namespace Application.IntegrationTests.Attendence;

public sealed class CreateAttendenceRecordCommandHandlerTests : AttendenceTestBase
{
    private const int EmployeeBadge = 100;
    private const string EmployeeId = "emp-1";

    public CreateAttendenceRecordCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    private void SetupWorkDayEmployee(DateTime shiftStart, DateTime shiftEnd)
    {
        EmployeeApi.AttendanceResponse = Result<EmployeeReponseForAttendance>.Success(
            new EmployeeReponseForAttendance(
                EmployeeId,
                EmployeeWorkStatus.Work,
                shiftStart,
                shiftEnd,
                TimeSpan.FromHours(8)));
    }

    private void SetupRestDayEmployee(DateTime shiftEnd)
    {
        EmployeeApi.AttendanceResponse = Result<EmployeeReponseForAttendance>.Success(
            new EmployeeReponseForAttendance(
                EmployeeId,
                EmployeeWorkStatus.Rest,
                DateTime.MinValue,
                shiftEnd,
                TimeSpan.FromHours(8)));
    }

    private async Task SeedPunchAsync(IAttendanceDbContext db, DateTime punchAt)
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
    public async Task Handle_WhenWorkDay_NoOpenRecord_CreatesCheckInRecord()
    {
        var today = DateTime.UtcNow.Date;
        var shiftStart = today.AddHours(9);
        var shiftEnd = today.AddHours(17);
        SetupWorkDayEmployee(shiftStart, shiftEnd);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var punch = today.AddHours(9).AddMinutes(15);
        await SeedPunchAsync(db, punch);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var saved = await db.AttendanceRecords.SingleOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(EmployeeId, saved!.EmployeeId);
        Assert.Null(saved.CheckOutAt);
    }

    [Fact]
    public async Task Handle_WhenWorkDay_TwoPunches_CreatesCheckInAndCheckOut()
    {
        var today = DateTime.UtcNow.Date;
        var shiftStart = today.AddHours(9);
        var shiftEnd = today.AddHours(17);
        SetupWorkDayEmployee(shiftStart, shiftEnd);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var checkInPunch = today.AddHours(8);
        var checkOutPunch = today.AddHours(18);
        await SeedPunchAsync(db, checkInPunch);
        await SeedPunchAsync(db, checkOutPunch);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOutPunch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(checkOutPunch, record.CheckOutAt);
    }

    [Fact]
    public async Task Handle_WhenRestDay_CreatesRecord()
    {
        var today = DateTime.UtcNow.Date;
        var shiftEnd = today.AddHours(17);
        SetupRestDayEmployee(shiftEnd);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var punch = today.AddHours(10);
        await SeedPunchAsync(db, punch);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.ToListAsync();
        Assert.Single(records);
    }

    [Fact]
    public async Task Handle_WhenWorkDay_ThreePunches_CreatesCheckInAndCheckOutAndNewCheckIn()
    {
        var today = DateTime.UtcNow.Date;
        var shiftStart = today.AddHours(9);
        var shiftEnd = today.AddHours(17);
        SetupWorkDayEmployee(shiftStart, shiftEnd);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var punch1 = today.AddHours(8);
        var punch2 = today.AddHours(17);
        var punch3 = today.AddHours(20);
        await SeedPunchAsync(db, punch1);
        await SeedPunchAsync(db, punch2);
        await SeedPunchAsync(db, punch3);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch3),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.ToListAsync();
        Assert.Equal(2, records.Count);
    }
}
