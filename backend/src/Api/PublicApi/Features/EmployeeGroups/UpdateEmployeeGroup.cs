using FluentValidation;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Features.EmployeeGroups;

namespace PublicApi.Features.EmployeeGroups;

public static class UpdateEmployeeGroup
{
    public sealed class Validator : AbstractValidator<UpdateEmployeeGroupCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100)
                .When(x => x.Name is not null);
        }
    }

    public sealed class Handler(
        IEmployeeGroupRepository repository,
        IEmployeeDbContext dbContext,
        IValidator<UpdateEmployeeGroupCommand> validator)
        : ICommandHandler<UpdateEmployeeGroupCommand, EmployeeGroupResponse>
    {
        public async Task<Result<EmployeeGroupResponse>> Handle(
            UpdateEmployeeGroupCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var group = await repository.GetByIdWithDetailsAsync(new EmployeeGroupId(command.Id), cancellationToken);
            if (group is null)
            {
                return Result<EmployeeGroupResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            if (command.Name is not null && command.Name != group.Name)
            {
                var nameExists = await repository.ExistsByNameAsync(command.Name, cancellationToken);
                if (nameExists)
                {
                    return Result<EmployeeGroupResponse>.Failure(EmployeeGroupErrors.InvalidName);
                }
            }

            group.UpdateDetails(command.Name, command.IsSecurity, command.Description);

            await dbContext.SaveChangesAsync(cancellationToken);

            var response = MapToResponse(group);
            return Result<EmployeeGroupResponse>.Success(response);
        }

        private static EmployeeGroupResponse MapToResponse(EmployeeGroup group)
        {
            var workSchedules = group.WorkSchedules.Select(ws => new WorkScheduleResponse(
                ws.Id.Value,
                ws.EmployeeGroupId.Value,
                ws.ShiftStartTime,
                ws.ShiftEndTime,
                ws.BreakStartTime,
                ws.BreakEndTime,
                ws.EndDayOffset,
                ws.AllowedCheckInLatenessMinutes,
                ws.AllowedCheckOutEarlinessMinutes,
                ws.IsActive,
                ws.CreatedOnUtc)).ToList();

            var rotationEntries = group.RotationEntries.Select(re => new RotationEntryResponse(
                re.Id.Value,
                re.EmployeeGroupId.Value,
                re.Position,
                re.WorkScheduleId?.Value,
                re.Status.ToString())).ToList();

            return new EmployeeGroupResponse(
                group.Id.Value,
                group.Name,
                group.IsSecurity,
                group.Description,
                group.RotationStartDate,
                group.NumberOfRotations,
                workSchedules,
                rotationEntries,
                group.CreatedOnUtc);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("employee-groups/{id:guid}", async (
                Guid id,
                UpdateEmployeeGroupRequest request,
                ICommandHandler<UpdateEmployeeGroupCommand, EmployeeGroupResponse> handler,
                CancellationToken ct) =>
            {
                var command = new UpdateEmployeeGroupCommand(id, request.Name, request.IsSecurity, request.Description);
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Update employee group metadata")
            .WithDescription("Updates employee group name, security flag, or description. Does not modify schedules or rotations.")
            .Produces<EmployeeGroupResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("UpdateEmployeeGroup");
        }
    }
}