

using Modules.Shared.Results;

namespace Modules.Employees.Contracts;

public interface IEmployeeApi
{
    Task<Result<EmployeeResponse>> GetEmployeeByBadgeAsync(int badge, DateOnly punchDate, CancellationToken ct = default);
    Task<Result<EmployeeResponse>> GetEmployeeByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<EmployeeResponse>>> GetEmployeesByBadgesAsync(
        IReadOnlyCollection<int> badges,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<EmployeeResponse>>> GetEmployeesByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken ct = default);

    Task<Result<WorkScheduleReadDto>> GetEmployeeWorkSchedule(Guid employeeGroupId, CancellationToken ct = default);

    Task<Result<EmployeeReponseForAttendance>> GetEmployeeForAttendance(int badge, DateOnly punchDate, CancellationToken ct = default);
}
public sealed record EmployeeReponseForAttendance(string EmployeeId,
    EmployeeWorkStatus WorkStatus,
    DateTime ShiftStartDateTime,
    DateTime ShiftEndDateTime,
    TimeSpan WorkTime);
public sealed record EmployeeResponse(
    string EmployeeId,
    int Bgd,
    string? FullName,
    WorkScheduleReadDto Schedule);
public sealed record WorkSchedule(
    TimeSpan StandardWorkTime,
    DateTime ExpectedCheckOutTime,
    DateTime ExpectedCheckInTime);
public sealed record WorkScheduleReadDto(
   Guid Id,
    Guid EmployeeGroupId,
    TimeOnly ShiftStartTime,
    TimeOnly ShiftEndTime,
    TimeSpan WorkTime,
    int EndDayOffset,
    TimeOnly BreakStartTime,
    TimeOnly BreakEndTime,
    int AllowedCheckInLatenessMinutes,
    int AllowedCheckOutEarlinessMinutes,
    bool IsActive,
    DateTime ShiftStartDateTime,
    DateTime ShiftEndtDateTime,
    DateTime BreakStartDateTime,
    DateTime BreakEndDateTime,
    EmployeeWorkStatus WorkStatus
);
public enum EmployeeWorkStatus
{
    Work = 1,
    Rest = 2
};
