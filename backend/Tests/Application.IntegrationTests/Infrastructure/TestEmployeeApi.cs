using Modules.Employees.Contracts;
using Modules.Shared.Results;

namespace Application.IntegrationTests.Infrastructure;

public sealed class TestEmployeeApi : IEmployeeApi
{
    public Result<EmployeeResponse> Response { get; set; } =
        Result<EmployeeResponse>.Failure(EmployeeErrors.NotFound);

    public Task<Result<EmployeeResponse>> GetEmployeeByBadgeAsync(
        int badge,
        CancellationToken ct = default)
    {
        return Task.FromResult(Response);
    }

    public Task<Result<EmployeeResponse>> GetEmployeeByIdAsync(
        string id,
        CancellationToken ct = default)
    {
        return Task.FromResult(Response);
    }
}
