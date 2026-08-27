using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Employees.Application.EmployeeGroups.Rotations;

public static class UpdateRotationPosition
{
    public sealed class Validator : AbstractValidator<UpdateRotationCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NewPosition)
                .GreaterThanOrEqualTo(1)
                .When(x => x.NewPosition.HasValue);
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

            var group = await repository.GetByIdWithDetailsAsync(
                new EmployeeGroupId(command.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            var entry = group.RotationEntries
                .FirstOrDefault(re => re.Position == command.Position);
            if (entry is null)
            {
                return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.RotationEntryNotFound);
            }

            var targetPosition = command.NewPosition ?? command.Position;
            if (targetPosition != command.Position &&
                group.RotationEntries.Any(re => re.Position == targetPosition))
            {
                return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.DuplicateRotationPosition);
            }

            WorkScheduleId? wsId = null;
            if (command.WorkScheduleId.HasValue)
            {
                var schedule = group.WorkSchedules
                    .FirstOrDefault(ws => ws.Id == new WorkScheduleId(command.WorkScheduleId.Value));
                if (schedule is null)
                {
                    return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.WorkScheduleNotFound);
                }
                wsId = schedule.Id;
            }

            var updated = group.ReplaceRotationEntry(command.Position, targetPosition, wsId);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<RotationEntryResponse>.Success(EmployeeGroupMapper.ToResponse(updated));
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
                var command = new UpdateRotationCommand(
                    groupId,
                    position,
                    request.NewPosition,
                    request.WorkScheduleId);
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Update rotation entry")
            .WithDescription("Replaces a rotation entry. newPosition is optional (defaults to current position); workScheduleId is a full-replacement field — send a Guid to make it a work day, or null/omit it to make it a rest day.")
            .Produces<RotationEntryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("UpdateRotationPosition");
        }
    }
}