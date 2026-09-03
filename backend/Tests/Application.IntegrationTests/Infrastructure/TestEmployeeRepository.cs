using Modules.Employees.Application.Abstractions;

namespace Application.IntegrationTests.Infrastructure;

public sealed class TestEmployeeRepository : IEmployeeRepository
{
    private readonly Dictionary<string, EmployeeDto> _employeesByBadge = new();
    private readonly Dictionary<string, EmployeeDto> _employeesById = new();

    public void AddEmployee(EmployeeDto employee)
    {
        _employeesByBadge[employee.Bdge] = employee;
        _employeesById[employee.EmployeeId] = employee;
    }

    public void Clear()
    {
        _employeesByBadge.Clear();
        _employeesById.Clear();
    }

    public Task<EmployeeDto?> GetEmployeeByIdAsync(string employeeId, CancellationToken ct = default)
    {
        _employeesById.TryGetValue(employeeId, out var employee);
        return Task.FromResult(employee);
    }

    public Task<EmployeeDto?> GetEmployeeByBgdeAsync(string bdge, CancellationToken ct = default)
    {
        _employeesByBadge.TryGetValue(bdge, out var employee);
        return Task.FromResult(employee);
    }

    public Task<IReadOnlyList<EmployeeDto>> GetEmployeesByBgdesAsync(
        IEnumerable<string> bdges, CancellationToken ct = default)
    {
        var result = bdges
            .Where(b => _employeesByBadge.ContainsKey(b))
            .Select(b => _employeesByBadge[b])
            .ToList();
        return Task.FromResult<IReadOnlyList<EmployeeDto>>(result);
    }

    public Task<IReadOnlyList<EmployeeDto>> GetEmployeesByIdsAsync(
        IEnumerable<string> ids, CancellationToken ct = default)
    {
        var result = ids
            .Where(id => _employeesById.ContainsKey(id))
            .Select(id => _employeesById[id])
            .ToList();
        return Task.FromResult<IReadOnlyList<EmployeeDto>>(result);
    }
}
