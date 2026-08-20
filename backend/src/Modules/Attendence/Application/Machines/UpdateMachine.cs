using FluentValidation;
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

public static class UpdateMachine
{
    public sealed record Request(
        string IpAddress,
        int Port);

    public sealed record Command(
        Guid MachineId,
        string IpAddress,
        int Port) : ICommand<Response>;

    public sealed record Response(
        Guid MachineId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.MachineId)
                .NotEmpty();
            RuleFor(x => x.IpAddress)
                .NotEmpty();
            RuleFor(x => x.Port)
                .GreaterThan(0);
        }
    }

    public sealed class CommandHandler(
        IAttendanceDbContext db,
        IValidator<Command> validator)
        : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(
            Command command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var machine = await db.Machines
                .FirstOrDefaultAsync(
                    m => m.Id == MachineId.From(command.MachineId),
                    cancellationToken);

            if (machine is null)
            {
                return Result<Response>.Failure(
                    MachineErrors.MachineNotFound(command.MachineId));
            }

            var exists = await db.Machines
                           .AnyAsync(e => e.IpAddress == command.IpAddress && e.Id != command.MachineId);
            if (exists)
            {
                return Result<Response>.Failure(
                    MachineErrors.MachineAlreadyExists(command.IpAddress));
            }

            machine.Update(command.IpAddress, command.Port);
            await db.SaveChangesAsync(cancellationToken);

            return Result<Response>.Success(new Response(machine.Id));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("attendance/machines/{id:guid}", async (
                Guid id,
                Request request,
                ICommandHandler<Command, Response> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new Command(
                    id,
                    request.IpAddress,
                    request.Port);
                var result = await handler.Handle(command, cancellationToken);
                return result.IsSuccess ? Results.NoContent()
                : result.Problem();
            })
            .WithTags("Attendance")
            .WithSummary("Update an attendance machine")
            .WithDescription("Updates the IP address and port of an existing attendance machine.");
        }
    }
}