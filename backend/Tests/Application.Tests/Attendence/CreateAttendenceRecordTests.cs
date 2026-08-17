using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.AttendenceRerords;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Employees.Contracts;
using Modules.Shared.Results;
using Moq;
using ContractSchedule = Modules.Employees.Contracts.WorkSchedule;
using DomainSchedule = Modules.Attendence.Domain.AttendenceRecords.WorkSchedule;

namespace Application.Tests.Attendence;

public sealed class CreateAttendenceRecordTests
{
    private static readonly MachineId MachineId = MachineId.New();

    private static readonly EmployeeResponse Employee = new(
        "emp-1",
        100,
        new ContractSchedule(
            StandardWorkTime: TimeSpan.FromHours(8),
            ExpectedCheckOutTime: new DateTime(2026, 8, 13, 17, 0, 0, DateTimeKind.Utc),
            ExpectedCheckInTime: new DateTime(2026, 8, 13, 9, 0, 0, DateTimeKind.Utc)));

    private static (CreateAttendenceRecord.CommandHandler handler, AttendanceDbContext db, Mock<IEmployeeApi> employeeApi) Arrange(
        int employeeBadge = 100)
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        var employeeApi = new Mock<IEmployeeApi>();
        employeeApi
            .Setup(x => x.GetEmployeeByBadgeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EmployeeResponse>.Success(Employee));

        return (new CreateAttendenceRecord.CommandHandler(employeeApi.Object, db), db, employeeApi);
    }

    private static AttendanceRecord SeedOpenRecord(AttendanceDbContext db)
    {
        var record = AttendanceRecord.Create(
            MachineId,
            Employee.EmployeeId,
            new DateTime(2026, 8, 13, 8, 0, 0, DateTimeKind.Utc));
        record.RegisterCheckIn(
            new DateTime(2026, 8, 13, 8, 0, 0, DateTimeKind.Utc),
            Employee.Schedule.ExpectedCheckInTime);

        db.AttendanceRecords.Add(record);
        db.SaveChanges();
        return record;
    }

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ReturnsFailure()
    {
        var (handler, _, employeeApi) = Arrange();
        employeeApi
            .Setup(x => x.GetEmployeeByBadgeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EmployeeResponse>.Failure(EmployeeErrors.NotFound));

        var command = new CreateAttendenceRecord.Command(100, MachineId, DateTime.UtcNow);

        var result = await handler.Handle(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenNoOpenRecord_CreatesAndPersistsNewCheckInRecord()
    {
        var (handler, db, _) = Arrange();
        var punch = new DateTime(2026, 8, 13, 9, 15, 0, DateTimeKind.Utc);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(100, MachineId, punch));

        Assert.True(result.IsSuccess);
        Assert.Single(db.AttendanceRecords);
        var saved = db.AttendanceRecords.Single();
        Assert.Equal(Employee.EmployeeId, saved.EmployeeId);
        Assert.Equal(punch, saved.CheckInAt);
        Assert.Null(saved.CheckOutAt);
        Assert.Equal(TimeSpan.FromMinutes(15), saved.LateTime);
    }

    [Fact]
    public async Task Handle_WhenOpenRecordExists_RegistersCheckOutAndComputesTimes()
    {
        var (handler, db, _) = Arrange();
        SeedOpenRecord(db);

        var punch = new DateTime(2026, 8, 13, 18, 0, 0, DateTimeKind.Utc);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(100, MachineId, punch));

        Assert.True(result.IsSuccess);
        var record = db.AttendanceRecords.Single();
        Assert.Equal(punch, record.CheckOutAt);
        Assert.Equal(TimeSpan.FromHours(10), record.WorkedTime);
        Assert.Equal(TimeSpan.FromHours(2), record.Overtime);
        Assert.Equal(TimeSpan.Zero, record.EarlyLeaveTime);
    }

    [Fact]
    public async Task Handle_WhenOpenRecordExists_ComputesEarlyLeave()
    {
        var (handler, db, _) = Arrange();
        SeedOpenRecord(db);

        var punch = new DateTime(2026, 8, 13, 16, 0, 0, DateTimeKind.Utc);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(100, MachineId, punch));

        Assert.True(result.IsSuccess);
        var record = db.AttendanceRecords.Single();
        Assert.Equal(TimeSpan.FromHours(8), record.WorkedTime);
        Assert.Equal(TimeSpan.Zero, record.Overtime);
        Assert.Equal(TimeSpan.FromHours(1), record.EarlyLeaveTime);
    }

    [Fact]
    public async Task Handle_WhenLastRecordAlreadyCheckedOut_CreatesNewCheckInRecord()
    {
        var (handler, db, _) = Arrange();
        var completed = SeedOpenRecord(db);
        completed.RegisterCheckOut(
            new DateTime(2026, 8, 13, 17, 0, 0, DateTimeKind.Utc),
            new DomainSchedule(TimeSpan.FromHours(8), new DateTime(2026, 8, 13, 17, 0, 0, DateTimeKind.Utc)));
        db.SaveChanges();

        var punch = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);

        var result = await handler.Handle(
            new CreateAttendenceRecord.Command(100, MachineId, punch));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, db.AttendanceRecords.Count());
        var newest = db.AttendanceRecords
            .OrderByDescending(x => x.CreatedOnUtc)
            .First();
        Assert.Null(newest.CheckOutAt);
    }
}