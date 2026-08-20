using Microsoft.EntityFrameworkCore;
using Moq;
using Modules.Attendence.Application.AttendenceRerords;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Attendence.Domain.Machines;
using Modules.Attendence.Infrastructure.Presistance;
using Modules.Employees.Contracts;
using Modules.Shared.Paginations.OffSet;
using Modules.Shared.Results;
using ContractSchedule = Modules.Employees.Contracts.WorkSchedule;
using DomainWorkSchedule = Modules.Attendence.Domain.AttendenceRecords.WorkSchedule;

namespace Application.Tests.Attendence;

public sealed class GetAllAttendanceRecordsTests
{
    private static readonly MachineId MachineId = MachineId.New();

    private static EmployeeResponse Employee(string employeeId, string fullName) =>
        new(
            employeeId,
            100,
            fullName,
            new ContractSchedule(
                StandardWorkTime: TimeSpan.FromHours(8),
                ExpectedCheckOutTime: DateTime.UtcNow.Date.AddHours(17),
                ExpectedCheckInTime: DateTime.UtcNow.Date.AddHours(9)));

    private static AttendanceRecord CreateRecord(
        string employeeId,
        DateTime checkIn,
        DateTime? checkOut = null)
    {
        var record = AttendanceRecord.Create(MachineId, employeeId);
        record.RegisterCheckIn(checkIn, checkIn.AddHours(-1), null);
        if (checkOut is not null)
        {
            record.RegisterCheckOut(
                checkOut.Value,
                new DomainWorkSchedule(TimeSpan.FromHours(8), checkOut.Value));
        }
        return record;
    }

    private static (GetAllAttendanceRecords.QueryHandler handler, Mock<IEmployeeApi> employeeApi) Arrange(
        IReadOnlyList<AttendanceRecord>? records = null,
        IReadOnlyList<EmployeeResponse>? employees = null)
    {
        var options = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AttendanceDbContext(options);

        db.AttendanceRecords.AddRange(records ?? []);
        db.SaveChanges();

        var employeeApi = new Mock<IEmployeeApi>();
        employeeApi
            .Setup(x => x.GetEmployeesByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<EmployeeResponse>>.Success(
                employees ?? new List<EmployeeResponse>()));

        return (new GetAllAttendanceRecords.QueryHandler(db, employeeApi.Object), employeeApi);
    }

    [Fact]
    public async Task Handle_WhenNoRecords_ReturnsRecordsNotFound()
    {
        var (handler, _) = Arrange();
        var query = TableRequest<GetAllAttendanceRecords.Response>.Create(10, 1);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("AttendanceRecord.RecordsNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenRecordsExist_ReturnsMappedResponses()
    {
        var checkIn = new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc);
        var checkOut = new DateTime(2026, 8, 20, 16, 0, 0, DateTimeKind.Utc);
        var record = CreateRecord("E100", checkIn, checkOut);
        var (handler, _) = Arrange(records: [record], employees: [Employee("E100", "John Doe")]);

        var result = await handler.Handle(
            TableRequest<GetAllAttendanceRecords.Response>.Create(10, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = result.Value.Item.Single();
        Assert.Equal(record.Id, item.AttendanceRecordId);
        Assert.Equal("E100", item.EmployeeId);
        Assert.Equal("John Doe", item.EmployeeFullName);
        Assert.Equal(checkIn, item.CheckInAt);
        Assert.Equal(checkOut, item.CheckOutAt);
        Assert.Equal(TimeSpan.FromHours(8), item.WorkedTime);
        Assert.False(item.IsAbsent);
    }

    [Fact]
    public async Task Handle_WhenEmployeeLookupFails_StillReturnsRecordsWithNullEmployee()
    {
        var checkIn = new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc);
        var record = CreateRecord("E100", checkIn);
        var (handler, employeeApi) = Arrange(records: [record]);
        employeeApi
            .Setup(x => x.GetEmployeesByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<EmployeeResponse>>.Failure(EmployeeErrors.NotFound));

        var result = await handler.Handle(
            TableRequest<GetAllAttendanceRecords.Response>.Create(10, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = result.Value.Item.Single();
        Assert.Equal("E100", item.EmployeeId);
        Assert.Null(item.EmployeeFullName);
    }

    [Fact]
    public async Task Handle_WhenSearchMatchesEmployeeFullName_FiltersResults()
    {
        var recordA = CreateRecord("E100", new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));
        var recordB = CreateRecord("E200", new DateTime(2026, 8, 20, 8, 5, 0, DateTimeKind.Utc));
        var (handler, _) = Arrange(
            records: [recordA, recordB],
            employees:
            [
                Employee("E100", "Alice Smith"),
                Employee("E200", "Bob Jones")
            ]);

        var result = await handler.Handle(
            TableRequest<GetAllAttendanceRecords.Response>.Create(10, 1, "bob"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = result.Value.Item.Single();
        Assert.Equal("Bob Jones", item.EmployeeFullName);
    }

    [Fact]
    public async Task Handle_SortByCheckInAtDesc_OrdersResults()
    {
        var recordA = CreateRecord("E100", new DateTime(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc));
        var recordB = CreateRecord("E200", new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc));
        var (handler, _) = Arrange(records: [recordA, recordB]);

        var result = await handler.Handle(
            TableRequest<GetAllAttendanceRecords.Response>.Create(10, 1, null, "checkinat", "desc"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var items = result.Value.Item.ToList();
        Assert.Equal(recordB.Id, items[0].AttendanceRecordId);
        Assert.Equal(recordA.Id, items[1].AttendanceRecordId);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsOnlyPageItems()
    {
        var records = Enumerable.Range(1, 3)
            .Select(i => CreateRecord(
                $"E{i}",
                new DateTime(2026, 8, 20, 8, i, 0, DateTimeKind.Utc)))
            .ToList();
        var (handler, _) = Arrange(records: records);

        var result = await handler.Handle(
            TableRequest<GetAllAttendanceRecords.Response>.Create(2, 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Page);
        Assert.Single(result.Value.Item);
        Assert.True(result.Value.HasPreviousPage);
        Assert.False(result.Value.HasNextPage);
    }
}