using Modules.Employees.Contracts;
using Modules.Shared.Results;

namespace Modules.Employees;

internal sealed class EmployeeApi : IEmployeeApi
{
    public Task<Result<EmployeeResponse>> GetEmployeeByBadgeAsync(
        int badge,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<EmployeeResponse>.Failure(EmployeeErrors.NotFound));
    }
}
