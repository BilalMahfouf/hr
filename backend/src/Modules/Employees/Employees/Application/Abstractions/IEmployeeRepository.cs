using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Employees.Application.Abstractions;

public sealed record EmployeeDto(
    string EmployeeId,
    string Bdge,
    string? EmployeeGroup,
    string? FullName);

public interface IEmployeeRepository
{
    Task<EmployeeDto?> GetEmployeeByIdAsync(string employeeId, CancellationToken ct = default);

    Task<EmployeeDto?> GetEmployeeByBgdeAsync(string bdge, CancellationToken ct = default);

    Task<IReadOnlyList<EmployeeDto>> GetEmployeesByBgdesAsync(
        IEnumerable<int> bdges,
        CancellationToken ct = default);

    Task<IReadOnlyList<EmployeeDto>> GetEmployeesByIdsAsync(
        IEnumerable<string> ids,
        CancellationToken ct = default);
}
