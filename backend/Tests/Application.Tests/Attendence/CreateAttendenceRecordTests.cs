using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.AttendenceRerords;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Attendence.Domain.Punches;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Employees.Contracts;
using Modules.Shared.Results;
using Moq;

namespace Application.Tests.Attendence;

public sealed class CreateAttendenceRecordTests
{
    private static readonly MachineId MachineId = MachineId.New();
    private const int EmployeeBadge = 100;
    private const string EmployeeId = "emp-1";

    private static (CreateAttendenceRecord.CommandHandler handler, AttendanceDbContext db, Mock<IEmployeeApi> employeeApi) Arrange()
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        var employeeApi = new Mock<IEmployeeApi>();

        return (new CreateAttendenceRecord.CommandHandler(employeeApi.Object, db), db, employeeApi);
    }

    private static void SetupWorkDayEmployee(Mock<IEmployeeApi> employeeApi, DateTime shiftStart, DateTime shiftEnd)
    {
        employeeApi
            .Setup(x => x.GetEmployeeForAttendance(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EmployeeReponseForAttendance>.Success(new EmployeeReponseForAttendance(
                EmployeeId,
                EmployeeWorkStatus.Work,
                shiftStart,
                shiftEnd,
                TimeSpan.FromHours(8))));
    }

    private static void SetupRestDayEmployee(Mock<IEmployeeApi> employeeApi, DateTime shiftEnd)
    {
        employeeApi
            .Setup(x => x.GetEmployeeForAttendance(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EmployeeReponseForAttendance>.Success(new EmployeeReponseForAttendance(
                EmployeeId,
                EmployeeWorkStatus.Rest,
                DateTime.MinValue,
                shiftEnd,
                TimeSpan.FromHours(8))));
    }

    private static void SetupEmployeeNotFound(Mock<IEmployeeApi> employeeApi)
    {
        employeeApi
            .Setup(x => x.GetEmployeeForAttendance(
                It.IsAny<int>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<EmployeeReponseForAttendance>.Failure(EmployeeErrors.NotFound));
    }

    private static async Task SeedPunchAsync(AttendanceDbContext db, int badge, DateTime punchAt)
    {
        db.Punches.Add(Punch.Create(MachineId, badge, punchAt, DateTime.UtcNow));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ReturnsFailure()
    {
        var (handler, _, employeeApi) = Arrange();
        SetupEmployeeNotFound(employeeApi);

        var command = new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, DateTime.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(EmployeeErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenWorkDay_NoOpenRecord_CreatesCheckInRecord()
    {
        var (handler, db, employeeApi) = Arrange();
        var today = DateTime.UtcNow.Date;
        var shiftStart = today.AddHours(9);
        var shiftEnd = today.AddHours(17);
        SetupWorkDayEmployee(employeeApi, shiftStart, shiftEnd);

        var punch = today.AddHours(9).AddMinutes(15);
        await SeedPunchAsync(db, EmployeeBadge, punch);

        var command = new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(db.AttendanceRecords);
        var saved = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(EmployeeId, saved.EmployeeId);
        Assert.Null(saved.CheckOutAt);
    }

    [Fact]
    public async Task Handle_WhenWorkDay_TwoPunches_CreatesCheckInAndCheckOut()
    {
        var (handler, db, employeeApi) = Arrange();
        var today = DateTime.UtcNow.Date;
        var shiftStart = today.AddHours(9);
        var shiftEnd = today.AddHours(17);
        SetupWorkDayEmployee(employeeApi, shiftStart, shiftEnd);

        var checkInPunch = today.AddHours(8);
        var checkOutPunch = today.AddHours(18);
        await SeedPunchAsync(db, EmployeeBadge, checkInPunch);
        await SeedPunchAsync(db, EmployeeBadge, checkOutPunch);

        var command = new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, checkOutPunch);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var record = await db.AttendanceRecords.SingleAsync();
        Assert.Equal(checkOutPunch, record.CheckOutAt);
    }

    [Fact]
    public async Task Handle_WhenRestDay_CreatesRecord()
    {
        var (handler, db, employeeApi) = Arrange();
        var today = DateTime.UtcNow.Date;
        var shiftEnd = today.AddHours(17);
        SetupRestDayEmployee(employeeApi, shiftEnd);

        var punch = today.AddHours(10);
        await SeedPunchAsync(db, EmployeeBadge, punch);

        var command = new CreateAttendenceRecord.Command(EmployeeBadge, MachineId, punch);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(db.AttendanceRecords);
    }
}
