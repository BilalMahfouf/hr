using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Employees.Application.Abstractions;

public sealed record EmployeeDto(string EmployeeId, int Bdge, int EmployeeGroup);

public interface IEmployeeRepository
{
    Task<EmployeeDto?> GetEmployeeByIdAsync(string employeeId, CancellationToken ct = default);

    Task<EmployeeDto?> GetEmployeeByBgdeAsync(int bdge, CancellationToken ct = default);
}
