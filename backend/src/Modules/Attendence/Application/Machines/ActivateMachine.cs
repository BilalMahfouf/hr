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

public static class ActivateMachine
{
    public sealed record Command(Guid MachineId) : ICommand;

    public sealed class CommandHandler(IAttendanceDbContext db)
        : ICommandHandler<Command>
    {
        public async Task<Result> Handle(
            Command command,
            CancellationToken cancellationToken = default)
        {
            if (command.MachineId == Guid.Empty)
            {
                return Result.Failure(
                    MachineErrors.MachineNotFound(command.MachineId));
            }

            var machineId = MachineId.From(command.MachineId);

            var machine = await db.Machines
                .FirstOrDefaultAsync(
                    m => m.Id == machineId,
                    cancellationToken);

            if (machine is null)
            {
                return Result.Failure(
                    MachineErrors.MachineNotFound(command.MachineId));
            }

            machine.Activate();
            await db.SaveChangesAsync(cancellationToken);

            return Result.Success;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("attendance/machines/{id:guid}/activate", async (
                Guid id,
                ICommandHandler<Command> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new Command(id), cancellationToken);
                return result.IsSuccess ? Results.NoContent()
                : result.Problem();
            })
            .WithTags("Attendance")
            .WithSummary("Activate an attendance machine")
            .WithDescription("Activates an attendance machine so it is picked up by the import endpoint.");
        }
    }
}