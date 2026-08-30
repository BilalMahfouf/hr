using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.CQRS;

namespace Modules.Employees.Application.EmployeeGroups;

public sealed record EmployeeGroupResponse(
    Guid Id,
    string EmployeeGroupNumber,
    string Name,
    bool IsSecurity,
    string? Description,
    DateOnly RotationStartDate,
    int NumberOfRotations,
    IReadOnlyList<WorkScheduleResponse> WorkSchedules,
    IReadOnlyList<RotationEntryResponse> RotationEntries,
    DateTime CreatedOnUtc);

public sealed record WorkScheduleResponse(
    Guid Id,
    Guid EmployeeGroupId,
    TimeOnly ShiftStartTime,
    TimeOnly ShiftEndTime,
    TimeOnly BreakStartTime,
    TimeOnly BreakEndTime,
    int EndDayOffset,
    int AllowedCheckInLatenessMinutes,
    int AllowedCheckOutEarlinessMinutes,
    bool IsActive,
    DateTime CreatedOnUtc);

public sealed record RotationEntryResponse(
    Guid Id,
    Guid EmployeeGroupId,
    int Position,
    Guid? WorkScheduleId,
    string Status);

public sealed record CreateWorkScheduleRequest(
    TimeOnly ShiftStartTime,
    TimeOnly ShiftEndTime,
    TimeOnly BreakStartTime,
    TimeOnly BreakEndTime,
    int EndDayOffset,
    int AllowedCheckInLatenessMinutes,
    int AllowedCheckOutEarlinessMinutes) : IWorkSchedulePayload;

public sealed record CreateRotationEntryRequest(
    int Position,
    int? WorkScheduleIndex);

public sealed record CreateEmployeeGroupRequest(
    string EmployeeGroupNumber,
    string Name,
    bool IsSecurity,
    string? Description,
    DateOnly RotationStartDate,
    IReadOnlyList<CreateWorkScheduleRequest> WorkSchedules,
    IReadOnlyList<CreateRotationEntryRequest> RotationEntries);

public sealed record UpdateEmployeeGroupRequest(
    string? Name,
    bool? IsSecurity,
    string? Description);

public sealed record ReplaceSchedulesAndRotationsRequest(
    IReadOnlyList<CreateWorkScheduleRequest> WorkSchedules,
    IReadOnlyList<CreateRotationEntryRequest> RotationEntries);

public sealed record UpdateWorkScheduleRequest(
    TimeOnly ShiftStartTime,
    TimeOnly ShiftEndTime,
    TimeOnly BreakStartTime,
    TimeOnly BreakEndTime,
    int EndDayOffset,
    int AllowedCheckInLatenessMinutes,
    int AllowedCheckOutEarlinessMinutes) : IWorkSchedulePayload;

public sealed record CreateWorkRotationRequest(
    int Position,
    Guid WorkScheduleId);

public sealed record CreateRestRotationRequest(
    int Position);

public sealed record UpdateRotationRequest(
    int? NewPosition,
    Guid? WorkScheduleId);

public sealed record CreateEmployeeGroupCommand(
    string EmployeeGroupNumber,
    string Name,
    bool IsSecurity,
    string? Description,
    DateOnly RotationStartDate,
    IReadOnlyList<CreateWorkScheduleRequest> WorkSchedules,
    IReadOnlyList<CreateRotationEntryRequest> RotationEntries) : ICommand<EmployeeGroupResponse>;

public sealed record GetEmployeeGroupByIdQuery(Guid Id) : IQuery<EmployeeGroupResponse>;

public sealed record GetAllEmployeeGroupsQuery() : IQuery<IReadOnlyList<EmployeeGroupResponse>>;

public sealed record UpdateEmployeeGroupCommand(
    Guid Id,
    string? Name,
    bool? IsSecurity,
    string? Description) : ICommand<EmployeeGroupResponse>;

public sealed record ReplaceSchedulesAndRotationsCommand(
    Guid GroupId,
    IReadOnlyList<CreateWorkScheduleRequest> WorkSchedules,
    IReadOnlyList<CreateRotationEntryRequest> RotationEntries) : ICommand<EmployeeGroupResponse>;

public sealed record DeleteEmployeeGroupCommand(Guid Id) : ICommand;

public sealed record CreateWorkScheduleCommand(
    Guid EmployeeGroupId,
    TimeOnly ShiftStartTime,
    TimeOnly ShiftEndTime,
    TimeOnly BreakStartTime,
    TimeOnly BreakEndTime,
    int EndDayOffset,
    int AllowedCheckInLatenessMinutes,
    int AllowedCheckOutEarlinessMinutes)
    : IWorkSchedulePayload, ICommand<WorkScheduleResponse>;

public sealed record GetWorkScheduleByIdQuery(Guid EmployeeGroupId, Guid ScheduleId) : IQuery<WorkScheduleResponse>;

public sealed record UpdateWorkScheduleCommand(
    Guid EmployeeGroupId,
    Guid ScheduleId,
    TimeOnly ShiftStartTime,
    TimeOnly ShiftEndTime,
    TimeOnly BreakStartTime,
    TimeOnly BreakEndTime,
    int EndDayOffset,
    int AllowedCheckInLatenessMinutes,
    int AllowedCheckOutEarlinessMinutes)
    : IWorkSchedulePayload, ICommand<WorkScheduleResponse>;

public sealed record DeleteWorkScheduleCommand(Guid EmployeeGroupId, Guid ScheduleId) : ICommand;

public sealed record ActivateWorkScheduleCommand(Guid EmployeeGroupId, Guid ScheduleId) : ICommand<WorkScheduleResponse>;

public sealed record DeactivateWorkScheduleCommand(Guid EmployeeGroupId, Guid ScheduleId) : ICommand<WorkScheduleResponse>;

public sealed record GetAllRotationsQuery(Guid EmployeeGroupId) : IQuery<IReadOnlyList<RotationEntryResponse>>;

public sealed record CreateWorkRotationCommand(
    Guid EmployeeGroupId,
    int Position,
    Guid WorkScheduleId) : ICommand<RotationEntryResponse>;

public sealed record CreateRestRotationCommand(
    Guid EmployeeGroupId,
    int Position) : ICommand<RotationEntryResponse>;

public sealed record UpdateRotationCommand(
    Guid EmployeeGroupId,
    int Position,
    int? NewPosition,
    Guid? WorkScheduleId) : ICommand<RotationEntryResponse>;

public sealed record DeleteRotationCommand(Guid EmployeeGroupId, int Position) : ICommand;

public static class EmployeeGroupMapper
{
    public static EmployeeGroupResponse ToResponse(EmployeeGroup group) =>
        new(
            group.Id.Value,
            group.EmployeeGroupNumber,
            group.Name,
            group.IsSecurity,
            group.Description,
            group.RotationStartDate,
            group.NumberOfRotations,
            group.WorkSchedules.Select(ToResponse).ToList(),
            group.RotationEntries.Select(ToResponse).ToList(),
            group.CreatedOnUtc);

    public static WorkScheduleResponse ToResponse(WorkSchedule ws) =>
        new(
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
            ws.CreatedOnUtc);

    public static RotationEntryResponse ToResponse(RotationEntry re) =>
        new(
            re.Id.Value,
            re.EmployeeGroupId.Value,
            re.Position,
            re.WorkScheduleId?.Value,
            re.Status.ToString());
}