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

namespace Modules.Employees.Application.EmployeeGroups.WorkSchedules;

public static class CreateWorkSchedule
{
    public sealed class Validator : WorkSchedulePayloadValidator<CreateWorkScheduleCommand>
    {
    }

    public sealed class Handler(
        IEmployeeGroupRepository repository,
        IEmployeeDbContext dbContext,
        IValidator<CreateWorkScheduleCommand> validator)
        : ICommandHandler<CreateWorkScheduleCommand, WorkScheduleResponse>
    {
        public async Task<Result<WorkScheduleResponse>> Handle(
            CreateWorkScheduleCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var group = await repository.GetByIdWithDetailsAsync(
                new EmployeeGroupId(command.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result<WorkScheduleResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            group.AddWorkSchedule(new CreateWorkScheduleDto(
                group.Id,
                command.ShiftStartTime,
                command.ShiftEndTime,
                command.EndDayOffset,
                command.BreakStartTime,
                command.BreakEndTime,
                command.AllowedCheckInLatenessMinutes,
                command.AllowedCheckOutEarlinessMinutes));

            var schedule = group.WorkSchedules.Last();

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<WorkScheduleResponse>.Success(EmployeeGroupMapper.ToResponse(schedule));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("employee-groups/{groupId:guid}/work-schedules", async (
                Guid groupId,
                CreateWorkScheduleRequest request,
                ICommandHandler<CreateWorkScheduleCommand, WorkScheduleResponse> handler,
                CancellationToken ct) =>
            {
                var command = new CreateWorkScheduleCommand(
                    groupId,
                    request.ShiftStartTime,
                    request.ShiftEndTime,
                    request.BreakStartTime,
                    request.BreakEndTime,
                    request.EndDayOffset,
                    request.AllowedCheckInLatenessMinutes,
                    request.AllowedCheckOutEarlinessMinutes);

                var result = await handler.Handle(command, ct);
                return result.IsSuccess
                    ? Results.Created(
                        $"/api/v1/employee-groups/{groupId}/work-schedules/{result.Value.Id}",
                        result.Value)
                    : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Create work schedule for employee group")
            .WithDescription("Adds a new work schedule to an employee group.")
            .Produces<WorkScheduleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("CreateWorkSchedule");
        }
    }
}