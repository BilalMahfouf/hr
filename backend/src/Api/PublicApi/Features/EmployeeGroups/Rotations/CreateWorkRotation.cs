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

public static class CreateWorkRotation
{
    public sealed class Validator : AbstractValidator<CreateWorkRotationCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Position).GreaterThanOrEqualTo(1);
            RuleFor(x => x.WorkScheduleId).NotEqual(Guid.Empty);
        }
    }

    public sealed class Handler(
        IEmployeeGroupRepository repository,
        IEmployeeDbContext dbContext,
        IValidator<CreateWorkRotationCommand> validator)
        : ICommandHandler<CreateWorkRotationCommand, RotationEntryResponse>
    {
        public async Task<Result<RotationEntryResponse>> Handle(
            CreateWorkRotationCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var group = await repository.GetByIdWithDetailsAsync(new EmployeeGroupId(command.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            // Check if position already exists
            var existingAtPosition = group.RotationEntries.FirstOrDefault(re => re.Position == command.Position);
            if (existingAtPosition is not null)
            {
                return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.DuplicateRotationPosition);
            }

            // Check if work schedule exists in group
            var schedule = group.WorkSchedules.FirstOrDefault(ws => ws.Id == new WorkScheduleId(command.WorkScheduleId));
            if (schedule is null)
            {
                return Result<RotationEntryResponse>.Failure(EmployeeGroupErrors.WorkScheduleNotFound);
            }

            group.AddRotationEntry(command.Position, schedule.Id);

            await dbContext.SaveChangesAsync(cancellationToken);

            var entry = group.RotationEntries.First(re => re.Position == command.Position);
            var response = MapToResponse(entry);
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
            app.MapPost("employee-groups/{groupId:guid}/rotations/work", async (
                Guid groupId,
                CreateWorkRotationRequest request,
                ICommandHandler<CreateWorkRotationCommand, RotationEntryResponse> handler,
                CancellationToken ct) =>
            {
                var command = new CreateWorkRotationCommand(groupId, request.Position, request.WorkScheduleId);
                var result = await handler.Handle(command, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/v1/employee-groups/{groupId}/rotations/{request.Position}", result.Value)
                    : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Create work rotation entry")
            .WithDescription("Adds a work rotation entry (linked to a work schedule) at the specified position.")
            .Produces<RotationEntryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("CreateWorkRotation");
        }
    }
}