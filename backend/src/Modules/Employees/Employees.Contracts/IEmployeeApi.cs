

using Modules.Shared.Results;

namespace Modules.Employees.Contracts;

public interface IEmployeeApi
{
    Task<Result<EmployeeResponse>> GetEmployeeByBadgeAsync(int badge, CancellationToken ct = default);
}
public sealed record EmployeeResponse(string EmployeeId,WorkSchedule Schedule);
public sealed record WorkSchedule(
    TimeSpan StandardWorkTime,
    DateTime ExpectedCheckOutTime,
    DateTime ExpectedCheckInTime);