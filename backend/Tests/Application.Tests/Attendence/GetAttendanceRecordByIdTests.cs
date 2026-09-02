using Microsoft.EntityFrameworkCore;
using Moq;
using Modules.Attendence.Application.AttendenceRerords;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Employees.Contracts;
using Modules.Shared.Results;

namespace Application.Tests.Attendence;

public sealed class GetAttendanceRecordByIdTests
{
    private static readonly MachineId MachineId = MachineId.New();

    private static WorkScheduleReadDto DummySchedule() => new(
        Guid.NewGuid(), Guid.NewGuid(),
        new TimeOnly(9, 0), new TimeOnly(17, 0),
        TimeSpan.FromHours(8), 0,
        new TimeOnly(12, 0), new TimeOnly(13, 0),
        5, 5, true,
        DateTime.UtcNow.Date.AddHours(9), DateTime.UtcNow.Date.AddHours(17),
        DateTime.UtcNow.Date.AddHours(12), DateTime.UtcNow.Date.AddHours(13),
        EmployeeWorkStatus.Work);

    private static (GetAttendanceRecordById.QueryHandler handler, Mock<IEmployeeApi> employeeApi) Arrange(
        AttendanceRecord? record = null,
        Result<EmployeeResponse>? employeeResult = null)
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        if (record is not null)
        {
            db.AttendanceRecords.Add(record);
            db.SaveChanges();
        }

        var employeeApi = new Mock<IEmployeeApi>();
        employeeApi
            .Setup(x => x.GetEmployeeByIdAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(employeeResult ??
                Result<EmployeeResponse>.Success(new EmployeeResponse(
                    record?.EmployeeId ?? "E100",
                    100,
                    "John Doe",
                    DummySchedule())));

        return (new GetAttendanceRecordById.QueryHandler(db, employeeApi.Object), employeeApi);
    }

    private static AttendanceRecord CreateRecord(string employeeId)
    {
        var record = AttendanceRecord.Create(MachineId, employeeId);
        record.RegisterCheckIn(
            new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc),
            null);
        record.RegisterCheckOut(
            new DateTime(2026, 8, 20, 16, 0, 0, DateTimeKind.Utc),
            new Modules.Attendence.Domain.AttendenceRecords.WorkSchedule(
                TimeSpan.FromHours(8),
                new DateTime(2026, 8, 20, 16, 0, 0, DateTimeKind.Utc)));
        return record;
    }

    [Fact]
    public async Task Handle_WhenRecordExists_ReturnsMappedResponse()
    {
        var record = CreateRecord("E100");
        var (handler, _) = Arrange(record);

        var result = await handler.Handle(
            new GetAttendanceRecordById.Query(record.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(record.Id, result.Value.AttendanceRecordId);
        Assert.Equal("E100", result.Value.EmployeeId);
        Assert.Equal("John Doe", result.Value.EmployeeFullName);
        Assert.Equal(record.CheckInAt, result.Value.CheckInAt);
        Assert.Equal(record.CheckOutAt, result.Value.CheckOutAt);
        Assert.Equal(record.WorkedTime, result.Value.WorkedTime);
        Assert.False(result.Value.IsAbsent);
    }

    [Fact]
    public async Task Handle_WhenRecordDoesNotExist_ReturnsNotFound()
    {
        var (handler, _) = Arrange();

        var result = await handler.Handle(
            new GetAttendanceRecordById.Query(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendanceRecord.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenIdIsEmpty_ReturnsNotFound()
    {
        var (handler, _) = Arrange();

        var result = await handler.Handle(
            new GetAttendanceRecordById.Query(Guid.Empty),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendanceRecord.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ReturnsResponseWithNullEmployee()
    {
        var record = CreateRecord("E100");
        var (handler, _) = Arrange(
            record,
            Result<EmployeeResponse>.Failure(EmployeeErrors.NotFound));

        var result = await handler.Handle(
            new GetAttendanceRecordById.Query(record.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("E100", result.Value.EmployeeId);
        Assert.Null(result.Value.EmployeeFullName);
        Assert.Equal(record.WorkedTime, result.Value.WorkedTime);
    }
}
