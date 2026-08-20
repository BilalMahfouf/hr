using System.Linq.Expressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.Punches;
using Modules.Employees.Contracts;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Paginations.OffSet;
using Modules.Shared.Results;

namespace Modules.Attendence.Application.Punches;

public static class GetAllPunches
{
    public sealed record Response(
        Guid PunchId,
        Guid MachineId,
        string? MachineIp,
        string? EmployeeId,
        string? EmployeeFullName,
        DateTime PunchOccurredOnUtc,
        DateTime CreatedOnUtc);

    public sealed class QueryHandler(
        IAttendanceDbContext db,
        IEmployeeApi employeeApi)
        : IQueryHandler<TableRequest<Response>, OffSetPagedList<Response>>
    {
        public async Task<Result<OffSetPagedList<Response>>> Handle(
            TableRequest<Response> query,
            CancellationToken cancellationToken = default)
        {
            var count = await db.Punches.CountAsync(cancellationToken);
            if (count <= 0)
            {
                return Result<OffSetPagedList<Response>>
                    .Failure(PunchErrors.PunchesNotFound);
            }

            var punches = await db.Punches
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var machineIps = await db.Machines
                .AsNoTracking()
                .ToDictionaryAsync(
                    m => m.Id,
                    m => m.IpAddress,
                    cancellationToken);

            var employees = await ResolveEmployeesAsync(
                punches.Select(p => p.EmployeeBadge).Distinct().ToArray(),
                cancellationToken);

            var responses = punches
                .Select(p =>
                {
                    machineIps.TryGetValue(p.MachineId, out var machineIp);
                    employees.TryGetValue(p.EmployeeBadge, out var employee);
                    return new Response(
                        p.Id,
                        p.MachineId,
                        machineIp,
                        employee?.EmployeeId,
                        employee?.FullName,
                        p.PunchOccurredAt,
                        p.CreatedOnUtc);
                })
                .ToList();

            if (!string.IsNullOrWhiteSpace(query.search))
            {
                var search = query.search;
                responses = responses
                    .Where(r =>
                        (r.EmployeeFullName?.ToLower().Contains(search) ?? false) ||
                        (r.EmployeeId?.ToLower().Contains(search) ?? false) ||
                        (r.MachineIp?.ToLower().Contains(search) ?? false))
                    .ToList();
            }

            Expression<Func<Response, object?>>? orderBy = query.SortColumn?.ToLower() switch
            {
                "employeeid" => r => r.EmployeeId,
                "employeefullname" => r => r.EmployeeFullName,
                "machineip" => r => r.MachineIp,
                "punchoccurredonutc" => r => r.PunchOccurredOnUtc,
                "createdonutc" => r => r.CreatedOnUtc,
                _ => r => r.PunchOccurredOnUtc,
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

        private async Task<Dictionary<int, EmployeeResponse>> ResolveEmployeesAsync(
            int[] badges,
            CancellationToken cancellationToken)
        {
            if (badges.Length == 0)
            {
                return new Dictionary<int, EmployeeResponse>();
            }

            var result = await employeeApi.GetEmployeesByBadgesAsync(
                badges,
                cancellationToken);
            if (!result.IsSuccess || result.Value is null)
            {
                return new Dictionary<int, EmployeeResponse>();
            }

            return result.Value
                .Where(e => e.EmployeeId is not null)
                .ToDictionary(e => e.Bgd, e => e);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("attendance/punches", async (
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
            .WithSummary("Get all punches with pagination, search and sorting");
        }
    }
}