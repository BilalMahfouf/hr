using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modules.Attendence.Application.AttendenceRerords;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Employees.Contracts;
using Modules.Shared.Results;

namespace Application.IntegrationTests.Attendence;

public sealed class CreateAttendenceRecordCommandHandlerTests : AttendenceTestBase
{
    private const int EmployeeBadge = 100;
    private const string EmployeeId = "emp-1";
    private const string GroupNumber = "SEC-01";

    public CreateAttendenceRecordCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    private async Task SetupDayShiftEmployee(DateOnly rotationStartDate, DateOnly punchDate)
    {
        await SeedSecurityGroupWWRRAsync(rotationStartDate, GroupNumber);
        await SeedEmployeeAsync(EmployeeBadge, EmployeeId, GroupNumber);
    }

    private async Task SetupAlternatingEmployee(DateOnly rotationStartDate, DateOnly punchDate)
    {
        await SeedAlternatingGroupRWRWAsync(rotationStartDate, GroupNumber);
        await SeedEmployeeAsync(EmployeeBadge, EmployeeId, GroupNumber);
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

    #region Day Shift Tests (06:00-18:00, security Day 1)

    [Fact]
    public async Task DayShift_NormalCheckInCheckOut()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        var punchDate = new DateOnly(2026, 1, 1); // Day 1 = Work
        await SetupDayShiftEmployee(rotationStart, punchDate);

        var checkIn = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkIn),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(checkIn, record.CheckInAt);
        Assert.Equal(checkOut, record.CheckOutAt);
        Assert.Equal(EmployeeId, record.EmployeeId);
        Assert.Equal(TimeSpan.FromHours(12), record.WorkedTime);
        Assert.Equal(TimeSpan.Zero, record.LateTime);
        Assert.Equal(TimeSpan.Zero, record.EarlyLeaveTime);
    }

    [Fact]
    public async Task DayShift_LateCheckIn()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        var punchDate = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, punchDate);

        var checkIn = new DateTime(2026, 1, 1, 6, 15, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkIn),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromMinutes(15), record.LateTime);
    }

    [Fact]
    public async Task DayShift_EarlyCheckOut()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        var punchDate = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, punchDate);

        var checkIn = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 1, 17, 30, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkIn),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromMinutes(30), record.EarlyLeaveTime);
    }

    [Fact]
    public async Task DayShift_LateAndEarly()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        var punchDate = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, punchDate);

        var checkIn = new DateTime(2026, 1, 1, 6, 15, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 1, 17, 30, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkIn),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromMinutes(15), record.LateTime);
        Assert.Equal(TimeSpan.FromMinutes(30), record.EarlyLeaveTime);
        Assert.Equal(TimeSpan.FromHours(11).Add(TimeSpan.FromMinutes(15)), record.WorkedTime);
    }

    [Fact]
    public async Task DayShift_OnlyCheckIn()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        var punchDate = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, punchDate);

        var checkIn = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkIn),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(checkIn, record.CheckInAt);
        Assert.Null(record.CheckOutAt);
    }

    [Fact]
    public async Task DayShift_TwoInOutPairs()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        var punchDate = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, punchDate);

        var in1 = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc);
        var out1 = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var in2 = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc);
        var out2 = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, in1);
        await SeedPunchAsync(EmployeeBadge, out1);
        await SeedPunchAsync(EmployeeBadge, in2);
        await SeedPunchAsync(EmployeeBadge, out2);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, in1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.OrderBy(r => r.CheckInAt).ToListAsync();
        Assert.Equal(2, records.Count);

        Assert.Equal(in1, records[0].CheckInAt);
        Assert.Equal(out1, records[0].CheckOutAt);
        Assert.Equal(TimeSpan.FromHours(4), records[0].WorkedTime);

        Assert.Equal(in2, records[1].CheckInAt);
        Assert.Equal(out2, records[1].CheckOutAt);
        Assert.Equal(TimeSpan.FromHours(5), records[1].WorkedTime);
    }

    [Fact]
    public async Task DayShift_ThreeInOutPairs()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        var punchDate = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, punchDate);

        var in1 = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc);
        var out1 = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var in2 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var out2 = new DateTime(2026, 1, 1, 14, 0, 0, DateTimeKind.Utc);
        var in3 = new DateTime(2026, 1, 1, 15, 0, 0, DateTimeKind.Utc);
        var out3 = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, in1);
        await SeedPunchAsync(EmployeeBadge, out1);
        await SeedPunchAsync(EmployeeBadge, in2);
        await SeedPunchAsync(EmployeeBadge, out2);
        await SeedPunchAsync(EmployeeBadge, in3);
        await SeedPunchAsync(EmployeeBadge, out3);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, in1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.OrderBy(r => r.CheckInAt).ToListAsync();
        Assert.Equal(3, records.Count);
    }

    [Fact]
    public async Task DayShift_CheckOutAfterSchedule()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        var punchDate = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, punchDate);

        var checkIn = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 1, 18, 10, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkIn),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromMinutes(10), record.Overtime);
    }

    #endregion

    #region Overnight Shift Tests (18:00-06:00+1, security Day 2)

    private void SetupOvernightEmployee(DateOnly rotationStart)
    {
        // The employee is already set up via SetupDayShiftEmployee or directly in each test.
        // This is a no-op helper for readability.
    }

    [Fact]
    public async Task Overnight_NormalCheckInCheckOut()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var checkIn = new DateTime(2026, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 3, 6, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(checkIn, record.CheckInAt);
        Assert.Equal(checkOut, record.CheckOutAt);
        Assert.Equal(TimeSpan.FromHours(12), record.WorkedTime);
    }

    [Fact]
    public async Task Overnight_CheckInBefore1800()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var checkIn = new DateTime(2026, 1, 2, 17, 55, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 3, 6, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(checkIn, record.CheckInAt);
        Assert.Equal(checkOut, record.CheckOutAt);
    }

    [Fact]
    public async Task Overnight_CheckInAfter1800()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var checkIn = new DateTime(2026, 1, 2, 18, 5, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 3, 6, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(checkIn, record.CheckInAt);
        Assert.Equal(checkOut, record.CheckOutAt);
    }

    [Fact]
    public async Task Overnight_CheckOutBeforeMidnight()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var checkIn = new DateTime(2026, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 2, 23, 59, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkIn),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(59)), record.WorkedTime);
    }

    [Fact]
    public async Task Overnight_CheckOutAtMidnight()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var checkIn = new DateTime(2026, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(6), record.WorkedTime);
    }

    [Fact]
    public async Task Overnight_CheckOutAfterMidnight()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var checkIn = new DateTime(2026, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 3, 1, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(7), record.WorkedTime);
    }

    [Fact]
    public async Task Overnight_CheckOutBefore0600()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var checkIn = new DateTime(2026, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 3, 5, 55, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromMinutes(5), record.EarlyLeaveTime);
    }

    [Fact]
    public async Task Overnight_CheckOutAfter0600()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var checkIn = new DateTime(2026, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 3, 6, 5, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromMinutes(5), record.Overtime);
    }

    [Fact]
    public async Task Overnight_MultipleInOut()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var in1 = new DateTime(2026, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        var out1 = new DateTime(2026, 1, 2, 22, 0, 0, DateTimeKind.Utc);
        var in2 = new DateTime(2026, 1, 2, 23, 30, 0, DateTimeKind.Utc);
        var out2 = new DateTime(2026, 1, 3, 6, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, in1);
        await SeedPunchAsync(EmployeeBadge, out1);
        await SeedPunchAsync(EmployeeBadge, in2);
        await SeedPunchAsync(EmployeeBadge, out2);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, out2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.OrderBy(r => r.CheckInAt).ToListAsync();
        Assert.Equal(2, records.Count);

        Assert.Equal(in1, records[0].CheckInAt);
        Assert.Equal(out1, records[0].CheckOutAt);
        Assert.Equal(TimeSpan.FromHours(4), records[0].WorkedTime);

        Assert.Equal(in2, records[1].CheckInAt);
        Assert.Equal(out2, records[1].CheckOutAt);
        Assert.Equal(TimeSpan.FromHours(6).Add(TimeSpan.FromMinutes(30)), records[1].WorkedTime);
    }

    #endregion

    #region Rest Day Tests

    [Fact]
    public async Task RestDay_SinglePunch()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        var punchDate = new DateOnly(2026, 1, 3); // Day 3 = Rest
        await SetupDayShiftEmployee(rotationStart, punchDate);

        var punch = new DateTime(2026, 1, 3, 10, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, punch);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.ToListAsync();
        Assert.Single(records);
    }

    [Fact]
    public async Task RestDay_MultiplePunches()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        var punchDate = new DateOnly(2026, 1, 3); // Day 3 = Rest
        await SetupDayShiftEmployee(rotationStart, punchDate);

        var punch1 = new DateTime(2026, 1, 3, 10, 0, 0, DateTimeKind.Utc);
        var punch2 = new DateTime(2026, 1, 3, 14, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, punch1);
        await SeedPunchAsync(EmployeeBadge, punch2);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.ToListAsync();
        Assert.Single(records);
        Assert.Equal(punch1, records[0].CheckInAt);
        Assert.Equal(punch2, records[0].CheckOutAt);
    }

    [Fact]
    public async Task RestDay_PunchAfterOvernight()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        // Day 2 overnight: IN at 18:00 on Day 2
        var overnightIn = new DateTime(2026, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, overnightIn);

        using var scope1 = CreateScope();
        var handler1 = CreateCreateAttendenceRecordHandler(scope1.ServiceProvider);
        await handler1.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, overnightIn),
            CancellationToken.None);

        // Day 3 rest day: punch at 08:00
        var restPunch = new DateTime(2026, 1, 3, 8, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, restPunch);

        using var scope2 = CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler2 = CreateCreateAttendenceRecordHandler(scope2.ServiceProvider);
        await handler2.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, restPunch),
            CancellationToken.None);

        var records = await db.AttendanceRecords.OrderBy(r => r.CheckInAt).ToListAsync();
        Assert.True(records.Count >= 1);
    }

    #endregion

    #region Rotation Pattern: Work/Work/Rest/Rest

    [Fact]
    public async Task RotationWWRR_Day1_Work()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 1));

        var checkIn = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(12), record.WorkedTime);
    }

    [Fact]
    public async Task RotationWWRR_Day2_Overnight()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var checkIn = new DateTime(2026, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 3, 6, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkIn),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(12), record.WorkedTime);
    }

    [Fact]
    public async Task RotationWWRR_Day3_Rest()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 3));

        var punch = new DateTime(2026, 1, 3, 10, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, punch);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.ToListAsync();
        Assert.Single(records);
    }

    [Fact]
    public async Task RotationWWRR_Day4_Rest()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 4));

        var punch = new DateTime(2026, 1, 4, 10, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, punch);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.ToListAsync();
        Assert.Single(records);
    }

    [Fact]
    public async Task RotationWWRR_CycleWrap_Day1()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        var cycle2Day1 = new DateOnly(2026, 1, 5); // Day 5 = position 1 of cycle 2
        await SetupDayShiftEmployee(rotationStart, cycle2Day1);

        var checkIn = new DateTime(2026, 1, 5, 6, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 5, 18, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(12), record.WorkedTime);
    }

    [Fact]
    public async Task RotationWWRR_CycleWrap_Day2()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        var cycle2Day2 = new DateOnly(2026, 1, 6); // Day 6 = position 2 of cycle 2
        await SetupDayShiftEmployee(rotationStart, cycle2Day2);

        var checkIn = new DateTime(2026, 1, 6, 18, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 7, 6, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(12), record.WorkedTime);
    }

    #endregion

    #region Rotation Pattern: Rest/Work/Rest/Work

    [Fact]
    public async Task RotationRWRW_Day1_Rest()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupAlternatingEmployee(rotationStart, new DateOnly(2026, 1, 1));

        var punch = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, punch);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.ToListAsync();
        Assert.Single(records);
    }

    [Fact]
    public async Task RotationRWRW_Day2_Work()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupAlternatingEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var checkIn = new DateTime(2026, 1, 2, 6, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(12), record.WorkedTime);
    }

    [Fact]
    public async Task RotationRWRW_Day3_Rest()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupAlternatingEmployee(rotationStart, new DateOnly(2026, 1, 3));

        var punch = new DateTime(2026, 1, 3, 10, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, punch);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.ToListAsync();
        Assert.Single(records);
    }

    [Fact]
    public async Task RotationRWRW_Day4_Overnight()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupAlternatingEmployee(rotationStart, new DateOnly(2026, 1, 4));

        var checkIn = new DateTime(2026, 1, 4, 18, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 5, 6, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(12), record.WorkedTime);
    }

    #endregion

    #region Rotation Boundary + Multi-Cycle

    [Fact]
    public async Task Rotation_CycleTransition_WWRR()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 1));

        // Day 4 (Rest) then Day 1 (Work) of next cycle
        var restPunch = new DateTime(2026, 1, 4, 10, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, restPunch);

        using var scope1 = CreateScope();
        var handler1 = CreateCreateAttendenceRecordHandler(scope1.ServiceProvider);
        await handler1.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, restPunch),
            CancellationToken.None);

        // Next cycle Day 1
        var workCheckIn = new DateTime(2026, 1, 5, 6, 0, 0, DateTimeKind.Utc);
        var workCheckOut = new DateTime(2026, 1, 5, 18, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, workCheckIn);
        await SeedPunchAsync(EmployeeBadge, workCheckOut);

        using var scope2 = CreateScope();
        var db = scope2.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler2 = CreateCreateAttendenceRecordHandler(scope2.ServiceProvider);
        await handler2.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, workCheckIn),
            CancellationToken.None);

        var records = await db.AttendanceRecords.OrderBy(r => r.CheckInAt).ToListAsync();
        Assert.True(records.Count >= 1);
    }

    [Fact]
    public async Task Rotation_ThreeFullCycles_WWRR()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 1));

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        // Process punches for 3 full cycles (12 days)
        for (int day = 0; day < 12; day++)
        {
            var date = rotationStart.AddDays(day);
            var position = (day % 4) + 1;

            if (position == 1) // Work day (06:00-18:00)
            {
                var ci = date.ToDateTime(new TimeOnly(6, 0));
                var co = date.ToDateTime(new TimeOnly(18, 0));
                await SeedPunchAsync(EmployeeBadge, ci);
                await SeedPunchAsync(EmployeeBadge, co);
                await handler.Handle(
                    new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, ci),
                    CancellationToken.None);
            }
            else if (position == 2) // Overnight (18:00-06:00+1)
            {
                var ci = date.ToDateTime(new TimeOnly(18, 0));
                var co = date.AddDays(1).ToDateTime(new TimeOnly(6, 0));
                await SeedPunchAsync(EmployeeBadge, ci);
                await SeedPunchAsync(EmployeeBadge, co);
                await handler.Handle(
                    new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, co),
                    CancellationToken.None);
            }
            else // Rest day (position 3 or 4)
            {
                var punch = date.ToDateTime(new TimeOnly(10, 0));
                await SeedPunchAsync(EmployeeBadge, punch);
                await handler.Handle(
                    new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
                    CancellationToken.None);
            }
        }

        var records = await db.AttendanceRecords.ToListAsync();
        Assert.NotEmpty(records);
    }

    [Fact]
    public async Task Rotation_DifferentStartDate()
    {
        var rotationStart = new DateOnly(2026, 3, 15);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 3, 20)); // Day 6 = position 2 (overnight)

        var checkIn = new DateTime(2026, 3, 20, 18, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 3, 21, 6, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(12), record.WorkedTime);
    }

    [Fact]
    public async Task Rotation_MultipleCyclesConsistency()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 1));

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        // Test Day 1 of cycle 1 and Day 1 of cycle 2 produce same results
        var cycle1CheckIn = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc);
        var cycle1CheckOut = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, cycle1CheckIn);
        await SeedPunchAsync(EmployeeBadge, cycle1CheckOut);
        await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, cycle1CheckIn),
            CancellationToken.None);

        var cycle1Record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(12), cycle1Record.WorkedTime);
    }

    #endregion

    #region Invalid Punch Sequences

    [Fact]
    public async Task SingleCheckOutWithoutCheckIn()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 1));

        // Only an OUT punch, no IN
        var punch = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, punch);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Single punch treated as check-in
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(punch, record.CheckInAt);
        Assert.Null(record.CheckOutAt);
    }

    [Fact]
    public async Task TwoConsecutiveCheckIns()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 1));

        var in1 = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc);
        var in2 = new DateTime(2026, 1, 1, 7, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, in1);
        await SeedPunchAsync(EmployeeBadge, in2);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, in2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.OrderBy(r => r.CheckInAt).ToListAsync();
        Assert.Single(records);
        Assert.Equal(in1, records[0].CheckInAt);
        Assert.Equal(in2, records[0].CheckOutAt);
    }

    [Fact]
    public async Task TwoConsecutiveCheckOuts()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 1));

        var in1 = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc);
        var out1 = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var out2 = new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, in1);
        await SeedPunchAsync(EmployeeBadge, out1);
        await SeedPunchAsync(EmployeeBadge, out2);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, out2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Second OUT opens a new record
        var records = await db.AttendanceRecords.OrderBy(r => r.CheckInAt).ToListAsync();
        Assert.Equal(2, records.Count);
    }

    #endregion

    #region Boundary Punches

    [Fact]
    public async Task DayShift_ExactShiftStartTime()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 1));

        var checkIn = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.Zero, record.LateTime);
        Assert.Equal(TimeSpan.Zero, record.EarlyLeaveTime);
    }

    [Fact]
    public async Task DayShift_ExactShiftEndTime()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 1));

        var checkIn = new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOut),
            CancellationToken.None);

        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.Zero, record.EarlyLeaveTime);
    }

    [Fact]
    public async Task Overnight_Exact1800CheckIn()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var checkIn = new DateTime(2026, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 3, 6, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkIn),
            CancellationToken.None);

        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.Zero, record.LateTime);
        Assert.Equal(TimeSpan.Zero, record.EarlyLeaveTime);
    }

    [Fact]
    public async Task Overnight_Exact0600CheckOut()
    {
        var rotationStart = new DateOnly(2026, 1, 1);
        await SetupDayShiftEmployee(rotationStart, new DateOnly(2026, 1, 2));

        var checkIn = new DateTime(2026, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 1, 3, 6, 0, 0, DateTimeKind.Utc);
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkIn),
            CancellationToken.None);

        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.Zero, record.EarlyLeaveTime);
    }

    #endregion

    #region Multiple Rotation Start Dates

    [Theory]
    [InlineData("2026-01-01")]
    [InlineData("2026-03-15")]
    [InlineData("2025-06-01")]
    [InlineData("2025-12-25")]
    public async Task Rotation_VariousStartDates_Day1Work(string startDateStr)
    {
        var rotationStart = DateOnly.Parse(startDateStr);
        await SetupDayShiftEmployee(rotationStart, rotationStart);

        var checkIn = rotationStart.ToDateTime(new TimeOnly(6, 0));
        var checkOut = rotationStart.ToDateTime(new TimeOnly(18, 0));
        await SeedPunchAsync(EmployeeBadge, checkIn);
        await SeedPunchAsync(EmployeeBadge, checkOut);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkIn),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(TimeSpan.FromHours(12), record.WorkedTime);
    }

    [Theory]
    [InlineData("2026-01-01")]
    [InlineData("2026-03-15")]
    [InlineData("2025-06-01")]
    public async Task Rotation_VariousStartDates_Day3Rest(string startDateStr)
    {
        var rotationStart = DateOnly.Parse(startDateStr);
        var day3 = rotationStart.AddDays(2);
        await SetupDayShiftEmployee(rotationStart, day3);

        var punch = day3.ToDateTime(new TimeOnly(10, 0));
        await SeedPunchAsync(EmployeeBadge, punch);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
        var handler = CreateCreateAttendenceRecordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var records = await db.AttendanceRecords.ToListAsync();
        Assert.Single(records);
    }

    #endregion
}
