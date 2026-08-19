using Carter;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Modules.Attendence.Application.Shared;
using Modules.Attendence.Domain.Machines;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Attendence.Application.Machines;

public static class CreateMachine
{
    public sealed record Command(
        string IpAddress,
        int MachineNumber,
        int? Port) : ICommand<Response>;

    public sealed record Response(
        Guid MachineId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.IpAddress)
                .NotEmpty();
            RuleFor(x => x.MachineNumber)
                .GreaterThan(0);
            RuleFor(x => x.Port)
                .GreaterThan(0)
                .When(x => x.Port.HasValue);
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

            var machine = AttendenceMachine.Create(
                MachineId.New(),
                command.IpAddress,
                command.MachineNumber,
                command.Port);

            db.Machines.Add(machine);
            await db.SaveChangesAsync(cancellationToken);

            return Result<Response>.Success(new Response(machine.Id));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("attendance/machines", async (
                [FromBody] Command command,
                ICommandHandler<Command, Response> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(command, cancellationToken);
                return result.IsSuccess ? Results.Created(
                    $"/attendance/machines/{result.Value.MachineId}",
                    result.Value)
                : result.Problem();
            })
            .WithTags("Attendance")
            .WithSummary("Register an attendance machine")
            .WithDescription("Creates a new attendance machine used by the import endpoint.");
        }
    }
}