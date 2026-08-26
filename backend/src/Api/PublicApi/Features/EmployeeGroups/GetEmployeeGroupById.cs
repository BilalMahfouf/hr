using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Features.EmployeeGroups;

namespace PublicApi.Features.EmployeeGroups;

public static class GetEmployeeGroupById
{
    public sealed class Handler(IEmployeeGroupRepository repository)
        : IQueryHandler<GetEmployeeGroupByIdQuery, EmployeeGroupResponse>
    {
        public async Task<Result<EmployeeGroupResponse>> Handle(
            GetEmployeeGroupByIdQuery query,
            CancellationToken cancellationToken = default)
        {
            var group = await repository.GetByIdWithDetailsAsync(new EmployeeGroupId(query.Id), cancellationToken);
            if (group is null)
            {
                return Result<EmployeeGroupResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

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
            app.MapGet("employee-groups/{id:guid}", async (
                Guid id,
                IQueryHandler<GetEmployeeGroupByIdQuery, EmployeeGroupResponse> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new GetEmployeeGroupByIdQuery(id), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Get employee group by ID")
            .WithDescription("Retrieves an employee group with all its work schedules and rotation entries.")
            .Produces<EmployeeGroupResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("GetEmployeeGroupById");
        }
    }
}