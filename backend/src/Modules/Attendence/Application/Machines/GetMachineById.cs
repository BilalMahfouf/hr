using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.Machines;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Attendence.Application.Machines;

public static class GetMachineById
{
    public sealed record Response(
        Guid MachineId,
        int MachineNumber,
        string IpAddress,
        int Port,
        bool IsActive);

    public sealed record Query(Guid MachineId) : IQuery<Response>;

    public sealed class QueryHandler(IAttendanceDbContext db)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(
            Query query,
            CancellationToken cancellationToken = default)
        {
            if (query.MachineId == Guid.Empty)
            {
                return Result<Response>.Failure(
                    MachineErrors.MachineNotFound(query.MachineId));
            }

            var machineId = MachineId.From(query.MachineId);

            var machine = await db.Machines
                .AsNoTracking()
                .Where(m => m.Id == machineId)
                .Select(m => new Response(
                    m.Id,
                    m.MachineNumber,
                    m.IpAddress,
                    m.Port,
                    m.IsActive))
                .FirstOrDefaultAsync(cancellationToken);

            if (machine is null)
            {
                return Result<Response>.Failure(
                    MachineErrors.MachineNotFound(query.MachineId));
            }

            return Result<Response>.Success(machine);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("attendance/machines/{id:guid}", async (
                Guid id,
                IQueryHandler<Query, Response> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new Query(id), cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value)
                : result.Problem();
            })
            .WithTags("Attendance")
            .WithSummary("Get attendance machine by ID")
            .WithDescription("Retrieves an attendance machine by its unique identifier.");
        }
    }
}
