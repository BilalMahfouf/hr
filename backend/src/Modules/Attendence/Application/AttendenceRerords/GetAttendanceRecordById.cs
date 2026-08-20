using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.AttendenceRecords;
using Modules.Employees.Contracts;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Attendence.Application.AttendenceRerords;

public static class GetAttendanceRecordById
{
    public sealed record Response(
        Guid AttendanceRecordId,
        string? EmployeeId,
        string? EmployeeFullName,
        DateTime CheckInAt,
        DateTime? CheckOutAt,
        TimeSpan WorkedTime,
        bool IsAbsent);

    public sealed record Query(Guid AttendanceRecordId) : IQuery<Response>;

    public sealed class QueryHandler(
        IAttendanceDbContext db,
        IEmployeeApi employeeApi)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(
            Query query,
            CancellationToken cancellationToken = default)
        {
            if (query.AttendanceRecordId == Guid.Empty)
            {
                return Result<Response>.Failure(
                    AttendanceRecordErrors.RecordNotFound(query.AttendanceRecordId));
            }

            var recordId = (AttendanceRecordId)query.AttendanceRecordId;

            var record = await db.AttendanceRecords
                .AsNoTracking()
                .Where(r => r.Id == recordId)
                .FirstOrDefaultAsync(cancellationToken);

            if (record is null)
            {
                return Result<Response>.Failure(
                    AttendanceRecordErrors.RecordNotFound(query.AttendanceRecordId));
            }

            EmployeeResponse? employee = null;
            var employeeResult = await employeeApi.GetEmployeeByIdAsync(
                record.EmployeeId,
                cancellationToken);
            if (employeeResult.IsSuccess)
            {
                employee = employeeResult.Value;
            }

            var response = new Response(
                record.Id,
                record.EmployeeId,
                employee?.FullName,
                record.CheckInAt,
                record.CheckOutAt,
                record.WorkedTime,
                record.IsAbsent);

            return Result<Response>.Success(response);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("attendance/records/{id:guid}", async (
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
            .WithSummary("Get attendance record by ID");
        }
    }
}