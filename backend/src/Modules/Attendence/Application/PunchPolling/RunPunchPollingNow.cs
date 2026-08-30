using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modules.Attendence.Application.Importer;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Attendence.Application.PunchPolling;

public static class RunPunchPollingNow
{
    public sealed record Command() : ICommand<Response>;

    public sealed record Response(
        int MachineCount,
        int PunchCount);

    public sealed class CommandHandler(
        ICommandHandler<ImportAttendanceLogs.Command, ImportAttendanceLogs.Response> importHandler)
        : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(
            Command command,
            CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var importCommand = new ImportAttendanceLogs.Command(today, today);

            var result = await importHandler.Handle(importCommand, cancellationToken);

            if (!result.IsSuccess)
                return Result<Response>.Failure(result.Error);

            return Result<Response>.Success(new Response(
                result.Value.MachineCount,
                result.Value.PunchCount));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("attendance/punch-polling/run", async (
                ICommandHandler<Command, Response> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new Command(), cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .WithTags("Attendance")
            .WithSummary("Manually trigger punch polling")
            .WithDescription("Immediately pulls punches from all active machines for today.");
        }
    }
}
