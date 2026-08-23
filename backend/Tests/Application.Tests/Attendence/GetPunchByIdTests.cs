//using Microsoft.EntityFrameworkCore;
//using Moq;
//using Modules.Attendence.Application.Punches;
//using Modules.Attendence.Domain.Machines;
//using Modules.Attendence.Domain.Punches;
//using Modules.Attendence.Infrastructure.Presistance;
//using Modules.Employees.Contracts;
//using Modules.Shared.Results;
//using ContractSchedule = Modules.Employees.Contracts.WorkSchedule;

//namespace Application.Tests.Attendence;

//public sealed class GetPunchByIdTests
//{
//    private static readonly MachineId MachineId = MachineId.New();

//    private static EmployeeResponse Employee(int badge, string employeeId, string fullName) =>
//        new(
//            employeeId,
//            badge,
//            fullName,
//            new ContractSchedule(
//                StandardWorkTime: TimeSpan.FromHours(8),
//                ExpectedCheckOutTime: DateTime.UtcNow.Date.AddHours(17),
//                ExpectedCheckInTime: DateTime.UtcNow.Date.AddHours(9)));

//    private static (GetPunchById.QueryHandler handler, Mock<IEmployeeApi> employeeApi) Arrange(
//        Punch? punch = null,
//        AttendenceMachine? machine = null,
//        Result<EmployeeResponse>? employeeResult = null)
//    {
//        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
//            .UseInMemoryDatabase(Guid.NewGuid().ToString())
//            .Options;
//        var db = new AttendanceDbContext(options);

//        if (punch is not null)
//        {
//            db.Punches.Add(punch);
//        }
//        if (machine is not null)
//        {
//            db.Machines.Add(machine);
//        }
//        db.SaveChanges();

//        var employeeApi = new Mock<IEmployeeApi>();
//        employeeApi
//            .Setup(x => x.GetEmployeeByBadgeAsync(
//                It.IsAny<int>(),
//                It.IsAny<CancellationToken>()))
//            .ReturnsAsync(employeeResult ??
//                Result<EmployeeResponse>.Success(Employee(punch?.EmployeeBadge ?? 0, "E100", "John Doe")));

//        return (new GetPunchById.QueryHandler(db, employeeApi.Object), employeeApi);
//    }

//    [Fact]
//    public async Task Handle_WhenPunchExists_ReturnsMappedResponse()
//    {
//        var occurredAt = new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc);
//        var punch = Punch.Create(MachineId, 100, occurredAt, occurredAt.AddMinutes(1));
//        var machine = AttendenceMachine.Create(MachineId, "192.168.3.205", 1, 8080);
//        var (handler, _) = Arrange(punch, machine);

//        var result = await handler.Handle(
//            new GetPunchById.Query(punch.Id),
//            CancellationToken.None);

//        Assert.True(result.IsSuccess);
//        Assert.Equal(punch.Id, result.Value.PunchId);
//        Assert.Equal(MachineId, result.Value.MachineId);
//        Assert.Equal("192.168.3.205", result.Value.MachineIp);
//        Assert.Equal("E100", result.Value.EmployeeId);
//        Assert.Equal("John Doe", result.Value.EmployeeFullName);
//        Assert.Equal(occurredAt, result.Value.PunchOccurredOnUtc);
//        Assert.Equal(punch.CreatedOnUtc, result.Value.CreatedOnUtc);
//    }

//    [Fact]
//    public async Task Handle_WhenPunchDoesNotExist_ReturnsNotFound()
//    {
//        var (handler, _) = Arrange();

//        var result = await handler.Handle(
//            new GetPunchById.Query(Guid.NewGuid()),
//            CancellationToken.None);

//        Assert.False(result.IsSuccess);
//        Assert.Equal("Punch.NotFound", result.Error.Code);
//    }

//    [Fact]
//    public async Task Handle_WhenIdIsEmpty_ReturnsNotFound()
//    {
//        var (handler, _) = Arrange();

//        var result = await handler.Handle(
//            new GetPunchById.Query(Guid.Empty),
//            CancellationToken.None);

//        Assert.False(result.IsSuccess);
//        Assert.Equal("Punch.NotFound", result.Error.Code);
//    }

//    [Fact]
//    public async Task Handle_WhenEmployeeNotFound_ReturnsResponseWithNullEmployee()
//    {
//        var occurredAt = new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc);
//        var punch = Punch.Create(MachineId, 100, occurredAt, occurredAt.AddMinutes(1));
//        var machine = AttendenceMachine.Create(MachineId, "192.168.3.205", 1, 8080);
//        var (handler, _) = Arrange(
//            punch,
//            machine,
//            Result<EmployeeResponse>.Failure(EmployeeErrors.NotFound));

//        var result = await handler.Handle(
//            new GetPunchById.Query(punch.Id),
//            CancellationToken.None);

//        Assert.True(result.IsSuccess);
//        Assert.Equal("192.168.3.205", result.Value.MachineIp);
//        Assert.Null(result.Value.EmployeeId);
//        Assert.Null(result.Value.EmployeeFullName);
//    }
//}