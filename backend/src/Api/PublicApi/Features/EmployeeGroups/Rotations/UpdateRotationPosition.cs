using FluentValidation;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Features.EmployeeGroups;

namespace PublicApi.Features.EmployeeGroups.Rotations;

public static class UpdateRotationPosition
{
    public sealed class Validator : AbstractValidator<UpdateRotationCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NewPosition)
                .GreaterThanOrEqualTo(1)
                .When(x => x.NewPosition.HasValue);

            RuleFor(x => x.WorkScheduleId)
                .NotEqual(Guid.Empty)
                .When(x => x.WorkScheduleId.HasValue);
        }
    }

    public sealed class Handler(
        IEmployeeGroupRepository repository,
        IEmployeeDbContext dbContext,
        IValidator<UpdateRotationCommand> validator)
        : ICommandHandler<UpdateRotationCommand, RotationEntryResponse>
    {
        public async Task<Result<RotationEntryResponse>> Handle(
            UpdateRotationCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var group = await repository.GetByIdWithDetailsAsync(new EmployeeGroupId(command.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            var entry = group.RotationEntries.FirstOrDefault(re => re.Position == command.Position);
            if (entry is null)
            {
                return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.RotationEntryNotFound);
            }

            // If new position provided, check uniqueness
            if (command.NewPosition.HasValue && command.NewPosition.Value != command.Position)
            {
                var existingAtNewPosition = group.RotationEntries.FirstOrDefault(re => re.Position == command.NewPosition.Value);
                if (existingAtNewPosition is not null)
                {
                    return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.DuplicateRotationPosition);
                }
            }

            // If workScheduleId provided, validate it exists in group
            WorkScheduleId? wsId = null;
            if (command.WorkScheduleId.HasValue)
            {
                var schedule = group.WorkSchedules.FirstOrDefault(ws => ws.Id == new WorkScheduleId(command.WorkScheduleId.Value));
                if (schedule is null)
                {
                    return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.WorkScheduleNotFound);
                }
                wsId = schedule.Id;
            }

            // Remove old entry and add new one at new position
            group.RemoveRotationEntry(command.Position);
            group.AddRotationEntry(command.NewPosition ?? command.Position, wsId);

            await dbContext.SaveChangesAsync(cancellationToken);

            var newPosition = command.NewPosition ?? command.Position;
            var updatedEntry = group.RotationEntries.First(re => re.Position == newPosition);
            var response = MapToResponse(updatedEntry);
            return Result<RotationEntryResponse>.Success(response);
        }

        private static RotationEntryResponse MapToResponse(RotationEntry re)
        {
            return new RotationEntryResponse(
                re.Id.Value,
                re.EmployeeGroupId.Value,
                re.Position,
                re.WorkScheduleId?.Value,
                re.Status.ToString());
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("employee-groups/{groupId:guid}/rotations/{position:int}", async (
                Guid groupId,
                int position,
                UpdateRotationRequest request,
                ICommandHandler<UpdateRotationCommand, RotationEntryResponse> handler,
                CancellationToken ct) =>
            {
                var command = new UpdateRotationCommand(groupId, position, request.NewPosition, request.WorkScheduleId);
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Update rotation entry")
            .WithDescription("Updates a rotation entry's position and/or work schedule reference.")
            .Produces<RotationEntryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("UpdateRotationPosition");
        }
    }
}