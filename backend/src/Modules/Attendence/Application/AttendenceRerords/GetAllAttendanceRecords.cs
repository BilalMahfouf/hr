using System.Linq.Expressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Employees.Contracts;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Paginations.OffSet;
using Modules.Shared.Results;

namespace Modules.Attendence.Application.AttendenceRerords;

public static class GetAllAttendanceRecords
{
    public sealed record Response(
        Guid AttendanceRecordId,
        string? EmployeeId,
        string? EmployeeFullName,
        DateTime CheckInAt,
        DateTime? CheckOutAt,
        TimeSpan WorkedTime,
        bool IsAbsent);

    public sealed class QueryHandler(
        IAttendanceDbContext db,
        IEmployeeApi employeeApi)
        : IQueryHandler<TableRequest<Response>, OffSetPagedList<Response>>
    {
        public async Task<Result<OffSetPagedList<Response>>> Handle(
            TableRequest<Response> query,
            CancellationToken cancellationToken = default)
        {
            var count = await db.AttendanceRecords.CountAsync(cancellationToken);
            if (count <= 0)
            {
                return Result<OffSetPagedList<Response>>
                    .Failure(AttendanceRecordErrors.RecordsNotFound);
            }

            var records = await db.AttendanceRecords
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var employees = await ResolveEmployeesAsync(
                records.Select(r => r.EmployeeId).Distinct().ToArray(),
                cancellationToken);

            var responses = records
                .Select(r =>
                {
                    employees.TryGetValue(r.EmployeeId, out var employee);
                    return new Response(
                        r.Id,
                        r.EmployeeId,
                        employee?.FullName,
                        r.CheckInAt,
                        r.CheckOutAt,
                        r.WorkedTime,
                        r.IsAbsent);
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(query.search))
            {
                var search = query.search;
                responses = responses
                    .Where(r =>
                        (r.EmployeeFullName?.ToLower().Contains(search) ?? false) ||
                        (r.EmployeeId?.ToLower().Contains(search) ?? false))
                    .ToList();
            }

            Expression<Func<Response, object?>>? orderBy = query.SortColumn?.ToLower() switch
            {
                "employeeid" => r => r.EmployeeId,
                "employeefullname" => r => r.EmployeeFullName,
                "checkinat" => r => r.CheckInAt,
                "checkoutat" => r => r.CheckOutAt,
                "workedtime" => r => r.WorkedTime,
                _ => r => r.CheckInAt,
            };

            var queryable = responses.AsQueryable();
            queryable = query.SortOrder?.ToLower() == "desc"
                ? queryable.OrderByDescending(orderBy)
                : queryable.OrderBy(orderBy);

            var items = queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var result = OffSetPagedList<Response>.Create(
                items,
                count,
                query.Page,
                query.PageSize);

            return Result<OffSetPagedList<Response>>.Success(result);
        }

        private async Task<Dictionary<string, EmployeeResponse>> ResolveEmployeesAsync(
            string[] employeeIds,
            CancellationToken cancellationToken)
        {
            if (employeeIds.Length == 0)
            {
                return new Dictionary<string, EmployeeResponse>();
            }

            var result = await employeeApi.GetEmployeesByIdsAsync(
                employeeIds,
                cancellationToken);
            if (!result.IsSuccess || result.Value is null)
            {
                return new Dictionary<string, EmployeeResponse>();
            }

            return result.Value
                .Where(e => e.EmployeeId is not null)
                .ToDictionary(e => e.EmployeeId, e => e);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("attendance/records", async (
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] string? sortColumn,
                [FromQuery] string? sortOrder,
                [FromQuery] string? search,
                IQueryHandler<TableRequest<Response>, OffSetPagedList<Response>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = TableRequest<Response>
                    .Create(pageSize, page, search, sortColumn, sortOrder);

                var result = await handler.Handle(query, cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value)
                : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("Attendance")
            .WithSummary("Get all attendance records with pagination, search and sorting");
        }
    }
}