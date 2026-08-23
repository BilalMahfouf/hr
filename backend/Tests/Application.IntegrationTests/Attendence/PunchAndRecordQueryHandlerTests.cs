//using Application.IntegrationTests.Infrastructure;
//using Application.IntegrationTests.TestBases;
//using Microsoft.Extensions.DependencyInjection;
//using Modules.Attendence.Application.AttendenceRerords;
//using Modules.Attendence.Application.Punches;
//using Modules.Attendence.Application.Shared;
//using Modules.Attendence.Domain.AttendenceRecords;
//using Modules.Attendence.Domain.Machines;
//using Modules.Attendence.Domain.Punches;
//using Modules.Employees.Contracts;
//using Modules.Shared.Paginations.OffSet;
//using Modules.Shared.Results;
//using ContractSchedule = Modules.Employees.Contracts.WorkSchedule;
//using DomainWorkSchedule = Modules.Attendence.Domain.AttendenceRecords.WorkSchedule;

//namespace Application.IntegrationTests.Attendence;

//public sealed class PunchAndRecordQueryHandlerTests : AttendenceTestBase
//{
//    public PunchAndRecordQueryHandlerTests(PostgresFixture fixture) : base(fixture)
//    {
//    }

//    private static GetAllPunches.QueryHandler CreateGetAllPunchesHandler(IServiceProvider services) =>
//        new(
//            services.GetRequiredService<IAttendanceDbContext>(),
//            services.GetRequiredService<IEmployeeApi>());

//    private static GetPunchById.QueryHandler CreateGetPunchByIdHandler(IServiceProvider services) =>
//        new(
//            services.GetRequiredService<IAttendanceDbContext>(),
//            services.GetRequiredService<IEmployeeApi>());

//    private static GetAllAttendanceRecords.QueryHandler CreateGetAllRecordsHandler(IServiceProvider services) =>
//        new(
//            services.GetRequiredService<IAttendanceDbContext>(),
//            services.GetRequiredService<IEmployeeApi>());

//    private static GetAttendanceRecordById.QueryHandler CreateGetRecordByIdHandler(IServiceProvider services) =>
//        new(
//            services.GetRequiredService<IAttendanceDbContext>(),
//            services.GetRequiredService<IEmployeeApi>());

//    private static EmployeeResponse Employee(int badge, string employeeId, string fullName) =>
//        new(
//            employeeId,
//            badge,
//            fullName,
//            new ContractSchedule(
//                StandardWorkTime: TimeSpan.FromHours(8),
//                ExpectedCheckOutTime: DateTime.UtcNow.Date.AddHours(17),
//                ExpectedCheckInTime: DateTime.UtcNow.Date.AddHours(9)));

//    private static async Task<AttendenceMachine> SeedMachineAsync(
//        IAttendanceDbContext db,
//        string ipAddress)
//    {
//        var machine = AttendenceMachine.Create(MachineId.New(), ipAddress, 1, 8080);
//        db.Machines.Add(machine);
//        await db.SaveChangesAsync();
//        return machine;
//    }

//    private static async Task<Punch> SeedPunchAsync(
//        IAttendanceDbContext db,
//        MachineId machineId,
//        int badge,
//        DateTime punchAt)
//    {
//        var punch = Punch.Create(machineId, badge, punchAt, punchAt.AddMinutes(1));
//        db.Punches.Add(punch);
//        await db.SaveChangesAsync();
//        return punch;
//    }

//    private static async Task<AttendanceRecord> SeedRecordAsync(
//        IAttendanceDbContext db,
//        MachineId machineId,
//        string employeeId,
//        DateTime checkIn,
//        DateTime? checkOut = null)
//    {
//        var record = AttendanceRecord.Create(machineId, employeeId);
//        record.RegisterCheckIn(checkIn, checkIn.AddHours(-1), null);
//        if (checkOut is not null)
//        {
//            record.RegisterCheckOut(
//                checkOut.Value,
//                new DomainWorkSchedule(TimeSpan.FromHours(8), checkOut.Value));
//        }
//        db.AttendanceRecords.Add(record);
//        await db.SaveChangesAsync();
//        return record;
//    }

//    [Fact]
//    public async Task GetAllPunches_WhenNoPunches_ReturnsPunchesNotFound()
//    {
//        using var scope = CreateScope();
//        var handler = CreateGetAllPunchesHandler(scope.ServiceProvider);

//        var result = await handler.Handle(
//            TableRequest<GetAllPunches.Response>.Create(10, 1),
//            CancellationToken.None);

//        Assert.False(result.IsSuccess);
//        Assert.Equal("Punch.PunchesNotFound", result.Error.Code);
//    }

//    [Fact]
//    public async Task GetAllPunches_WhenPunchesExist_ReturnsMappedResponses()
//    {
//        using var scope = CreateScope();
//        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
//        var handler = CreateGetAllPunchesHandler(scope.ServiceProvider);

//        var machineA = await SeedMachineAsync(db, "192.168.3.205");
//        var machineB = await SeedMachineAsync(db, "10.0.0.15");
//        var punchA = await SeedPunchAsync(db, machineA.Id, 100, new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));
//        var punchB = await SeedPunchAsync(db, machineB.Id, 200, new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc));

//        EmployeeApi.Employees =
//        [
//            Employee(100, "E100", "Alice Smith"),
//            Employee(200, "E200", "Bob Jones")
//        ];

//        var result = await handler.Handle(
//            TableRequest<GetAllPunches.Response>.Create(10, 1),
//            CancellationToken.None);

//        Assert.True(result.IsSuccess);
//        Assert.Equal(2, result.Value.TotalCount);
//        var items = result.Value.Item.OrderBy(i => i.PunchOccurredOnUtc).ToList();
//        Assert.Equal(punchA.Id, items[0].PunchId);
//        Assert.Equal("192.168.3.205", items[0].MachineIp);
//        Assert.Equal("E100", items[0].EmployeeId);
//        Assert.Equal("Alice Smith", items[0].EmployeeFullName);
//        Assert.Equal(punchB.Id, items[1].PunchId);
//        Assert.Equal("10.0.0.15", items[1].MachineIp);
//        Assert.Equal("Bob Jones", items[1].EmployeeFullName);
//    }

//    [Fact]
//    public async Task GetAllPunches_WhenSearchAndSortApplied_FiltersAndOrders()
//    {
//        using var scope = CreateScope();
//        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
//        var handler = CreateGetAllPunchesHandler(scope.ServiceProvider);

//        var machine = await SeedMachineAsync(db, "192.168.3.205");
//        await SeedPunchAsync(db, machine.Id, 100, new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));
//        await SeedPunchAsync(db, machine.Id, 200, new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc));

//        EmployeeApi.Employees =
//        [
//            Employee(100, "E100", "Alice Smith"),
//            Employee(200, "E200", "Bob Jones")
//        ];

//        var result = await handler.Handle(
//            TableRequest<GetAllPunches.Response>.Create(10, 1, "bob", "employeefullname", "asc"),
//            CancellationToken.None);

//        Assert.True(result.IsSuccess);
//        var item = result.Value.Item.Single();
//        Assert.Equal("Bob Jones", item.EmployeeFullName);
//    }

//    [Fact]
//    public async Task GetPunchById_WhenExists_ReturnsMappedResponse()
//    {
//        using var scope = CreateScope();
//        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
//        var handler = CreateGetPunchByIdHandler(scope.ServiceProvider);

//        var machine = await SeedMachineAsync(db, "192.168.3.205");
//        var punch = await SeedPunchAsync(db, machine.Id, 100, new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));

//        EmployeeApi.Response = Result<EmployeeResponse>.Success(Employee(100, "E100", "Alice Smith"));

//        var result = await handler.Handle(
//            new GetPunchById.Query(punch.Id),
//            CancellationToken.None);

//        Assert.True(result.IsSuccess);
//        Assert.Equal(punch.Id, result.Value.PunchId);
//        Assert.Equal("192.168.3.205", result.Value.MachineIp);
//        Assert.Equal("E100", result.Value.EmployeeId);
//        Assert.Equal("Alice Smith", result.Value.EmployeeFullName);
//    }

//    [Fact]
//    public async Task GetPunchById_WhenNotExists_ReturnsNotFound()
//    {
//        using var scope = CreateScope();
//        var handler = CreateGetPunchByIdHandler(scope.ServiceProvider);

//        var result = await handler.Handle(
//            new GetPunchById.Query(Guid.NewGuid()),
//            CancellationToken.None);

//        Assert.False(result.IsSuccess);
//        Assert.Equal("Punch.NotFound", result.Error.Code);
//    }

//    [Fact]
//    public async Task GetAllRecords_WhenRecordsExist_ReturnsMappedResponses()
//    {
//        using var scope = CreateScope();
//        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
//        var handler = CreateGetAllRecordsHandler(scope.ServiceProvider);

//        var machine = await SeedMachineAsync(db, "192.168.3.205");
//        var checkIn = new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc);
//        var checkOut = new DateTime(2026, 8, 20, 16, 0, 0, DateTimeKind.Utc);
//        var record = await SeedRecordAsync(db, machine.Id, "E100", checkIn, checkOut);

//        EmployeeApi.Employees = [Employee(100, "E100", "Alice Smith")];

//        var result = await handler.Handle(
//            TableRequest<GetAllAttendanceRecords.Response>.Create(10, 1),
//            CancellationToken.None);

//        Assert.True(result.IsSuccess);
//        var item = result.Value.Item.Single();
//        Assert.Equal(record.Id, item.AttendanceRecordId);
//        Assert.Equal("E100", item.EmployeeId);
//        Assert.Equal("Alice Smith", item.EmployeeFullName);
//        Assert.Equal(checkIn, item.CheckInAt);
//        Assert.Equal(checkOut, item.CheckOutAt);
//        Assert.Equal(TimeSpan.FromHours(8), item.WorkedTime);
//        Assert.False(item.IsAbsent);
//    }

//    [Fact]
//    public async Task GetAllRecords_WhenSearchMatchesEmployee_FiltersResults()
//    {
//        using var scope = CreateScope();
//        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
//        var handler = CreateGetAllRecordsHandler(scope.ServiceProvider);

//        var machine = await SeedMachineAsync(db, "192.168.3.205");
//        await SeedRecordAsync(db, machine.Id, "E100", new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));
//        await SeedRecordAsync(db, machine.Id, "E200", new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc));

//        EmployeeApi.Employees =
//        [
//            Employee(100, "E100", "Alice Smith"),
//            Employee(200, "E200", "Bob Jones")
//        ];

//        var result = await handler.Handle(
//            TableRequest<GetAllAttendanceRecords.Response>.Create(10, 1, "bob"),
//            CancellationToken.None);

//        Assert.True(result.IsSuccess);
//        var item = result.Value.Item.Single();
//        Assert.Equal("Bob Jones", item.EmployeeFullName);
//    }

//    [Fact]
//    public async Task GetRecordById_WhenExists_ReturnsMappedResponse()
//    {
//        using var scope = CreateScope();
//        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();
//        var handler = CreateGetRecordByIdHandler(scope.ServiceProvider);

//        var machine = await SeedMachineAsync(db, "192.168.3.205");
//        var record = await SeedRecordAsync(
//            db,
//            machine.Id,
//            "E100",
//            new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));

//        EmployeeApi.Response = Result<EmployeeResponse>.Success(Employee(100, "E100", "Alice Smith"));

//        var result = await handler.Handle(
//            new GetAttendanceRecordById.Query(record.Id),
//            CancellationToken.None);

//        Assert.True(result.IsSuccess);
//        Assert.Equal(record.Id, result.Value.AttendanceRecordId);
//        Assert.Equal("E100", result.Value.EmployeeId);
//        Assert.Equal("Alice Smith", result.Value.EmployeeFullName);
//    }

//    [Fact]
//    public async Task GetRecordById_WhenNotExists_ReturnsNotFound()
//    {
//        using var scope = CreateScope();
//        var handler = CreateGetRecordByIdHandler(scope.ServiceProvider);

//        var result = await handler.Handle(
//            new GetAttendanceRecordById.Query(Guid.NewGuid()),
//            CancellationToken.None);

//        Assert.False(result.IsSuccess);
//        Assert.Equal("AttendanceRecord.NotFound", result.Error.Code);
//    }
//}