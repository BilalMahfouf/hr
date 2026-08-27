using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Employees.Application.EmployeeGroups;

public static class ReplaceSchedulesAndRotations
{
    public sealed class Validator : AbstractValidator<ReplaceSchedulesAndRotationsCommand>
    {
        public Validator()
        {
            RuleForEach(x => x.WorkSchedules)
                .SetValidator(new WorkScheduleRequestValidator());

            RuleForEach(x => x.RotationEntries)
                .Must(r => r.Position >= 1)
                .WithMessage("Rotation position must be greater than or equal to 1.");

            RuleFor(x => x.RotationEntries)
                .Must(entries => entries.Select(e => e.Position).Distinct().Count() == entries.Count)
                .WithMessage("Rotation positions must be unique.")
                .When(x => x.RotationEntries.Count > 0);

            RuleFor(x => x.RotationEntries)
                .NotEmpty()
                .WithMessage("At least one rotation entry is required.");

            RuleFor(x => x)
                .Must(ValidateScheduleReferences)
                .WithMessage("Rotation entries reference work schedules that don't exist in the request.");
        }

        private static bool ValidateScheduleReferences(ReplaceSchedulesAndRotationsCommand c)
        {
            foreach (var entry in c.RotationEntries)
            {
                if (entry.WorkScheduleIndex.HasValue)
                {
                    if (entry.WorkScheduleIndex.Value < 0 || entry.WorkScheduleIndex.Value >= c.WorkSchedules.Count)
                        return false;
                }
            }
            return true;
        }
    }

    public sealed class Handler(
        IEmployeeGroupRepository repository,
        IEmployeeDbContext dbContext,
        IValidator<ReplaceSchedulesAndRotationsCommand> validator)
        : ICommandHandler<ReplaceSchedulesAndRotationsCommand, EmployeeGroupResponse>
    {
        public async Task<Result<EmployeeGroupResponse>> Handle(
            ReplaceSchedulesAndRotationsCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var group = await repository.GetByIdWithDetailsAsync(new EmployeeGroupId(command.GroupId), cancellationToken);
            if (group is null)
            {
                return Result<EmployeeGroupResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            group.ReplaceSchedulesAndRotations(
                command.WorkSchedules.Select(r => r.ToDto(group.Id)).ToList(),
                command.RotationEntries
                    .Select(r => (r.Position, r.WorkScheduleIndex))
                    .ToList());

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<EmployeeGroupResponse>.Success(EmployeeGroupMapper.ToResponse(group));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("employee-groups/{groupId:guid}/schedules-and-rotations", async (
                Guid groupId,
                ReplaceSchedulesAndRotationsRequest request,
                ICommandHandler<ReplaceSchedulesAndRotationsCommand, EmployeeGroupResponse> handler,
                CancellationToken ct) =>
            {
                var command = new ReplaceSchedulesAndRotationsCommand(
                    groupId,
                    request.WorkSchedules,
                    request.RotationEntries);
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Replace all schedules and rotations for a group")
            .WithDescription("Atomically replaces all work schedules and rotation entries for an employee group. Rotation entries reference schedules by their index in the workSchedules array.")
            .Produces<EmployeeGroupResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("ReplaceSchedulesAndRotations");
        }
    }
}