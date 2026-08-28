using Modules.Employees.Contracts;
using Modules.Shared.Results;

namespace Application.IntegrationTests.Infrastructure;

public sealed class TestEmployeeApi : IEmployeeApi
{
    public Result<EmployeeResponse> Response { get; set; } =
        Result<EmployeeResponse>.Failure(EmployeeErrors.NotFound);

    public IReadOnlyList<EmployeeResponse> Employees { get; set; } = [];

    public Task<Result<EmployeeResponse>> GetEmployeeByBadgeAsync(
        int badge,
        DateOnly punchDate,
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

    public Task<Result<IReadOnlyList<EmployeeResponse>>> GetEmployeesByBadgesAsync(
        IReadOnlyCollection<int> badges,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<IReadOnlyList<EmployeeResponse>>.Success(Employees));
    }

    public Task<Result<IReadOnlyList<EmployeeResponse>>> GetEmployeesByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<IReadOnlyList<EmployeeResponse>>.Success(Employees));
    }

    public Task<Result<WorkScheduleReadDto>> GetEmployeeWorkSchedule(Guid employeeGroupId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
