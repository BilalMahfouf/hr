using FluentValidation;
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

public static class UpdatePunchPollingSettings
{
    public sealed record Command(
        bool IsEnabled,
        int IntervalMinutes) : ICommand;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IntervalMinutes)
                .InclusiveBetween(10, 60);
        }
    }

    public sealed class CommandHandler(
        IAttendanceDbContext db,
        IValidator<Command> validator,
        IPunchPollingScheduler scheduler)
        : ICommandHandler<Command>
    {
        public async Task<Result> Handle(
            Command command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var settings = await db.PunchPollingSettings
                .FirstOrDefaultAsync(cancellationToken);

            if (settings is null)
            {
                settings = PunchPollingSettings.Create(
                    PunchPollingSettingsId.New(),
                    command.IsEnabled,
                    command.IntervalMinutes);

                db.PunchPollingSettings.Add(settings);
            }
            else
            {
                settings.Update(command.IsEnabled, command.IntervalMinutes);
            }

            await db.SaveChangesAsync(cancellationToken);

            await scheduler.ScheduleAsync(
                command.IsEnabled,
                command.IntervalMinutes,
                cancellationToken);

            return Result.Success;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("attendance/punch-polling", async (
                Command command,
                ICommandHandler<Command> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(command, cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            })
            .WithTags("Attendance")
            .WithSummary("Update punch polling settings")
            .WithDescription("Enables or disables automatic punch polling and sets the interval (10-60 minutes).");
        }
    }
}
