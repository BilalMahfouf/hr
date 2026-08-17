using Modules.Employees.Application.Abstractions;
using Modules.Employees.Contracts;
using Modules.Shared.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Employees.Application;

public sealed class EmployeeApi(IEmployeeRepository employeeRepo) : IEmployeeApi
{
    private WorkSchedule GetEmployeeWorkSchedule(string employeeGroup)
    {
        return employeeGroup switch
        {
            "1" => new WorkSchedule(TimeSpan.FromHours(8), DateTime.Today.AddHours(16), DateTime.Today.AddHours(8)),
            "2" => new WorkSchedule(TimeSpan.FromHours(7), DateTime.Today.AddHours(16), DateTime.Today.AddHours(9)),
            "3" => new WorkSchedule(TimeSpan.FromHours(6), DateTime.Today.AddHours(15), DateTime.Today.AddHours(9)),
            _ => new WorkSchedule(TimeSpan.FromHours(6), DateTime.Today.AddHours(15), DateTime.Today.AddHours(9))
        };
    }
    public async Task<Result<EmployeeResponse>> GetEmployeeByBadgeAsync(int badge, CancellationToken ct = default)
    {
        var employee = await employeeRepo.GetEmployeeByBgdeAsync(badge.ToString(), ct);
        if (employee is null)
        {
            return Result<EmployeeResponse>.Failure(EmployeeErrors.NotFound);
        }
        var workSchedule = GetEmployeeWorkSchedule(employee.EmployeeGroup);
        if(int.TryParse(employee.Bdge, out int bdg))
        {

        }
        var response = new EmployeeResponse(employee.EmployeeId, bdg , workSchedule);
        return Result<EmployeeResponse>.Success(response);
    }

    public async Task<Result<EmployeeResponse>> GetEmployeeByIdAsync(string id, CancellationToken ct = default)
    {
        var employee = await employeeRepo.GetEmployeeByIdAsync(id, ct);
        if (employee is null)
        {
            return Result<EmployeeResponse>.Failure(EmployeeErrors.NotFound);
        }
        var workSchedule = GetEmployeeWorkSchedule(employee.EmployeeGroup);
                if(int.TryParse(employee.Bdge, out int bdg))
        {

        }
 
        var response = new EmployeeResponse(employee.EmployeeId, bdg, workSchedule);
        return Result<EmployeeResponse>.Success(response);

    }
}
