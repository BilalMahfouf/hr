using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.PunchPolling;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Attendence.Application.PunchPolling;

public static class GetPunchPollingSettings
{
    public sealed record Response(
        bool IsEnabled,
        int IntervalMinutes,
        DateTime UpdatedAt);

    public sealed record Query() : IQuery<Response>;

    public sealed class QueryHandler(IAttendanceDbContext db)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(
            Query query,
            CancellationToken cancellationToken = default)
        {
            var settings = await db.PunchPollingSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (settings is null)
            {
                var defaultSettings = PunchPollingSettings.Create(
                    PunchPollingSettingsId.New(),
                    false,
                    30);

                return Result<Response>.Success(new Response(
                    defaultSettings.IsEnabled,
                    defaultSettings.IntervalMinutes,
                    defaultSettings.UpdatedAt));
            }

            return Result<Response>.Success(new Response(
                settings.IsEnabled,
                settings.IntervalMinutes,
                settings.UpdatedAt));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("attendance/punch-polling", async (
                IQueryHandler<Query, Response> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new Query(), cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .WithTags("Attendance")
            .WithSummary("Get punch polling settings")
            .WithDescription("Returns the current punch polling configuration.");
        }
    }
}
