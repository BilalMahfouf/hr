

using Modules.Shared.Results;

namespace Modules.Employees.Contracts;

public interface IEmployeeApi
{
    Task<Result<EmployeeResponse>> GetEmployeeByBadgeAsync(int badge, CancellationToken ct = default);
    Task<Result<EmployeeResponse>> GetEmployeeByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<EmployeeResponse>>> GetEmployeesByBadgesAsync(
        IReadOnlyCollection<int> badges,
        CancellationToken ct = default);
    Task<Result<IReadOnlyList<EmployeeResponse>>> GetEmployeesByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken ct = default);
}
public sealed record EmployeeResponse(
    string EmployeeId,
    int Bgd,
    string? FullName,
    WorkSchedule Schedule);
public sealed record WorkSchedule(
    TimeSpan StandardWorkTime,
    DateTime ExpectedCheckOutTime,
    DateTime ExpectedCheckInTime);