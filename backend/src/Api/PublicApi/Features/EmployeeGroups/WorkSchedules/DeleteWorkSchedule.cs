using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Features.EmployeeGroups;

namespace PublicApi.Features.EmployeeGroups.WorkSchedules;

public static class DeleteWorkSchedule
{
    public sealed class Handler(
        IEmployeeGroupRepository repository,
        IEmployeeDbContext dbContext)
        : ICommandHandler<DeleteWorkScheduleCommand>
    {
        public async Task<Result> Handle(
            DeleteWorkScheduleCommand command,
            CancellationToken cancellationToken = default)
        {
            var group = await repository.GetByIdWithDetailsAsync(new EmployeeGroupId(command.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result.Failure(EmployeeGroupErrors.NotFound);
            }

            var schedule = group.WorkSchedules.FirstOrDefault(ws => ws.Id == new WorkScheduleId(command.ScheduleId));
            if (schedule is null)
            {
                return Result.Failure(EmployeeGroupErrors.WorkScheduleNotFound);
            }

            // Check if schedule is referenced by rotation
            var isReferenced = group.RotationEntries.Any(re => re.WorkScheduleId == schedule.Id);
            if (isReferenced)
            {
                return Result.Failure(EmployeeGroupErrors.WorkScheduleInUse);
            }

            group.RemoveWorkSchedule(schedule.Id);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success;
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapDelete("employee-groups/{groupId:guid}/work-schedules/{scheduleId:guid}", async (
                Guid groupId,
                Guid scheduleId,
                ICommandHandler<DeleteWorkScheduleCommand> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new DeleteWorkScheduleCommand(groupId, scheduleId), ct);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Delete work schedule")
            .WithDescription("Deletes a work schedule. Cannot delete if schedule is referenced by rotation entries.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("DeleteWorkSchedule");
        }
    }
}