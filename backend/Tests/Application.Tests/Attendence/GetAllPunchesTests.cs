using Microsoft.EntityFrameworkCore;
using Moq;
using Modules.Attendence.Application.Punches;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Domain.Punches;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Employees.Contracts;
using Modules.Shared.Paginations.OffSet;
using Modules.Shared.Results;

namespace Application.Tests.Attendence;

public sealed class GetAllPunchesTests
{
    private static readonly MachineId MachineA = MachineId.New();
    private static readonly MachineId MachineB = MachineId.New();

    private static WorkScheduleReadDto DummySchedule() => new(
        Guid.NewGuid(), Guid.NewGuid(),
        new TimeOnly(9, 0), new TimeOnly(17, 0),
        TimeSpan.FromHours(8), 0,
        new TimeOnly(12, 0), new TimeOnly(13, 0),
        5, 5, true,
        DateTime.UtcNow.Date.AddHours(9), DateTime.UtcNow.Date.AddHours(17),
        DateTime.UtcNow.Date.AddHours(12), DateTime.UtcNow.Date.AddHours(13),
        EmployeeWorkStatus.Work);

    private static EmployeeResponse Employee(int badge, string employeeId, string fullName) =>
        new(
            employeeId,
            badge,
            fullName,
            DummySchedule());

    private static (GetAllPunches.QueryHandler handler, Mock<IEmployeeApi> employeeApi) Arrange(
        IReadOnlyList<Punch>? punches = null,
        IReadOnlyList<AttendenceMachine>? machines = null,
        IReadOnlyList<EmployeeResponse>? employees = null)
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        db.Punches.AddRange(punches ?? []);
        db.Machines.AddRange(machines ?? []);
        db.SaveChanges();

        var employeeApi = new Mock<IEmployeeApi>();
        employeeApi
            .Setup(x => x.GetEmployeesByBadgesAsync(
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<EmployeeResponse>>.Success(
                employees ?? new List<EmployeeResponse>()));

        return (new GetAllPunches.QueryHandler(db, employeeApi.Object), employeeApi);
    }

    private static Punch CreatePunch(MachineId machineId, int badge, DateTime occurredAt) =>
        Punch.Create(machineId, badge, occurredAt, occurredAt.AddMinutes(1));

    private static AttendenceMachine CreateMachine(MachineId id, string ip) =>
        AttendenceMachine.Create(id, ip, 1, MachineType.ZKTecoSdk, 8080);

    [Fact]
    public async Task Handle_WhenNoPunches_ReturnsEmptyPagedList()
    {
        var (handler, _) = Arrange();
        var query = TableRequest<GetAllPunches.Response>.Create(10, 1);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Empty(result.Value.Item);
    }

    [Fact]
    public async Task Handle_WhenPunchesExist_ReturnsMappedResponses()
    {
        var punch = CreatePunch(MachineA, 100, new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));
        var machine = CreateMachine(MachineA, "192.168.3.205");
        var (handler, _) = Arrange(
            punches: [punch],
            machines: [machine],
            employees: [Employee(100, "E100", "John Doe")]);

        var result = await handler.Handle(
            TableRequest<GetAllPunches.Response>.Create(10, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = result.Value.Item.Single();
        Assert.Equal(punch.Id, item.PunchId);
        Assert.Equal(MachineA, item.MachineId);
        Assert.Equal("192.168.3.205", item.MachineIp);
        Assert.Equal("E100", item.EmployeeId);
        Assert.Equal("John Doe", item.EmployeeFullName);
        Assert.Equal(punch.PunchOccurredAt, item.PunchOccurredOnUtc);
        Assert.Equal(punch.CreatedOnUtc, item.CreatedOnUtc);
    }

    [Fact]
    public async Task Handle_WhenEmployeeLookupFails_StillReturnsPunchesWithNullEmployee()
    {
        var punch = CreatePunch(MachineA, 100, new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));
        var machine = CreateMachine(MachineA, "192.168.3.205");
        var (handler, employeeApi) = Arrange(punches: [punch], machines: [machine]);
        employeeApi
            .Setup(x => x.GetEmployeesByBadgesAsync(
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<EmployeeResponse>>.Failure(EmployeeErrors.NotFound));

        var result = await handler.Handle(
            TableRequest<GetAllPunches.Response>.Create(10, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = result.Value.Item.Single();
        Assert.Equal("192.168.3.205", item.MachineIp);
        Assert.Null(item.EmployeeId);
        Assert.Null(item.EmployeeFullName);
    }

    [Fact]
    public async Task Handle_WhenMachineMissing_ReturnsNullMachineIp()
    {
        var punch = CreatePunch(MachineB, 100, new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));
        var (handler, _) = Arrange(
            punches: [punch],
            employees: [Employee(100, "E100", "John Doe")]);

        var result = await handler.Handle(
            TableRequest<GetAllPunches.Response>.Create(10, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Item.Single().MachineIp);
    }

    [Fact]
    public async Task Handle_WhenSearchMatchesEmployeeFullName_FiltersResults()
    {
        var punchA = CreatePunch(MachineA, 100, new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));
        var punchB = CreatePunch(MachineB, 200, new DateTime(2026, 8, 20, 8, 5, 0, DateTimeKind.Utc));
        var (handler, _) = Arrange(
            punches: [punchA, punchB],
            employees:
            [
                Employee(100, "E100", "Alice Smith"),
                Employee(200, "E200", "Bob Jones")
            ]);

        var result = await handler.Handle(
            TableRequest<GetAllPunches.Response>.Create(10, 1, "ali"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = result.Value.Item.Single();
        Assert.Equal("Alice Smith", item.EmployeeFullName);
    }

    [Fact]
    public async Task Handle_WhenSearchMatchesMachineIp_FiltersResults()
    {
        var punchA = CreatePunch(MachineA, 100, new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));
        var punchB = CreatePunch(MachineB, 200, new DateTime(2026, 8, 20, 8, 5, 0, DateTimeKind.Utc));
        var machineA = CreateMachine(MachineA, "192.168.3.205");
        var machineB = CreateMachine(MachineB, "10.0.0.15");
        var (handler, _) = Arrange(
            punches: [punchA, punchB],
            machines: [machineA, machineB]);

        var result = await handler.Handle(
            TableRequest<GetAllPunches.Response>.Create(10, 1, "3.205"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = result.Value.Item.Single();
        Assert.Equal("192.168.3.205", item.MachineIp);
    }

    [Fact]
    public async Task Handle_SortByEmployeeFullNameDesc_OrdersResults()
    {
        var punchA = CreatePunch(MachineA, 100, new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));
        var punchB = CreatePunch(MachineB, 200, new DateTime(2026, 8, 20, 8, 5, 0, DateTimeKind.Utc));
        var (handler, _) = Arrange(
            punches: [punchA, punchB],
            employees:
            [
                Employee(100, "E100", "Alice Smith"),
                Employee(200, "E200", "Bob Jones")
            ]);

        var result = await handler.Handle(
            TableRequest<GetAllPunches.Response>.Create(10, 1, null, "employeefullname", "desc"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var items = result.Value.Item.ToList();
        Assert.Equal("Bob Jones", items[0].EmployeeFullName);
        Assert.Equal("Alice Smith", items[1].EmployeeFullName);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsOnlyPageItems()
    {
        var punches = Enumerable.Range(1, 3)
            .Select(i => CreatePunch(
                MachineA,
                100 + i,
                new DateTime(2026, 8, 20, 8, i, 0, DateTimeKind.Utc)))
            .ToList();
        var (handler, _) = Arrange(punches: punches);

        var result = await handler.Handle(
            TableRequest<GetAllPunches.Response>.Create(2, 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Page);
        Assert.Single(result.Value.Item);
        Assert.True(result.Value.HasPreviousPage);
        Assert.False(result.Value.HasNextPage);
    }
}
