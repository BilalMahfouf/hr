using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.Punches;
using Modules.Employees.Contracts;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Attendence.Application.Punches;

public static class GetPunchById
{
    public sealed record Response(
        Guid PunchId,
        Guid MachineId,
        string? MachineIp,
        string? EmployeeId,
        string? EmployeeFullName,
        DateTime PunchOccurredOnUtc,
        DateTime CreatedOnUtc);

    public sealed record Query(Guid PunchId) : IQuery<Response>;

    public sealed class QueryHandler(
        IAttendanceDbContext db,
        IEmployeeApi employeeApi)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(
            Query query,
            CancellationToken cancellationToken = default)
        {
            if (query.PunchId == Guid.Empty)
            {
                return Result<Response>.Failure(
                    PunchErrors.PunchNotFound(query.PunchId));
            }

            var punchId = PunchId.From(query.PunchId);

            var punch = await db.Punches
                .AsNoTracking()
                .Where(p => p.Id == punchId)
                .FirstOrDefaultAsync(cancellationToken);

            if (punch is null)
            {
                return Result<Response>.Failure(
                    PunchErrors.PunchNotFound(query.PunchId));
            }

            var machineIp = await db.Machines
                .AsNoTracking()
                .Where(m => m.Id == punch.MachineId)
                .Select(m => m.IpAddress)
                .FirstOrDefaultAsync(cancellationToken);

            EmployeeResponse? employee = null;
            // to do refactor this to use new method instead of this .
            var employeeResult = await employeeApi.GetEmployeeByBadgeAsync(
                punch.EmployeeBadge, DateOnly.MinValue,
                cancellationToken);
            if (employeeResult.IsSuccess)
            {
                employee = employeeResult.Value;
            }

            var response = new Response(
                punch.Id,
                punch.MachineId,
                machineIp,
                employee?.EmployeeId,
                employee?.FullName,
                punch.PunchOccurredAt,
                punch.CreatedOnUtc);

            return Result<Response>.Success(response);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("attendance/punches/{id:guid}", async (
                Guid id,
                IQueryHandler<Query, Response> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new Query(id), cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value)
                : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("Attendance")
            .WithSummary("Get punch by ID");
        }
    }
}