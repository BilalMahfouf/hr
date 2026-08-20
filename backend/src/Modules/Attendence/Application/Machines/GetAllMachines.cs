using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Shared;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Attendence.Application.Machines;

public static class GetAllMachines
{
    public sealed record Response(
        Guid MachineId,
        int MachineNumber,
        string IpAddress,
        int Port,
        bool IsActive,
        DateTime CreatedOnUtc);

    public sealed record Query() : IQuery<IEnumerable<Response>>;

    public sealed class QueryHandler(IAttendanceDbContext db)
        : IQueryHandler<Query, IEnumerable<Response>>
    {
        public async Task<Result<IEnumerable<Response>>> Handle(
            Query query,
            CancellationToken cancellationToken = default)
        {
            var machines = await db.Machines
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var response = machines
                .Select(m => new Response(
                    m.Id,
                    m.MachineNumber,
                    m.IpAddress,
                    m.Port,
                    m.IsActive,
                    m.CreatedOnUtc));

            return Result<IEnumerable<Response>>.Success(response);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("attendance/machines", async (
                IQueryHandler<Query, IEnumerable<Response>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new Query(), cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value)
                : result.Problem();
            })
            .WithTags("Attendance")
            .WithSummary("Get all attendance machines");
        }
    }
}