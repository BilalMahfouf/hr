using Dapper;
using Microsoft.Data.SqlClient;
using Modules.Employees.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Employees.EnapPresistance;

internal class EnapRepository(ISqlConnectionFactory sqlConnectionFactory) : IEmployeeRepository
{

    private const string _employeeTableName = "T_EmPloyes";

    public async Task<EmployeeDto?> GetEmployeeByBgdeAsync(int bdge, CancellationToken ct = default)
    {
        using var conn = sqlConnectionFactory.CreateConnection();
        var query = @$"select Matricule,Bgd,CodeGrpP from {_employeeTableName} where Bdg=@bgde";
        var employee = await conn.QuerySingleOrDefaultAsync<EmployeeDto>(query, new
        {
            bdge
        });
        if (employee is null)
        {
            return null;
        }
        return employee;
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(string employeeId, CancellationToken ct = default)
    {
        using var conn = sqlConnectionFactory.CreateConnection();
        var query = @$"select Matricule,Bdg,CodeGrpP from {_employeeTableName} where Matricule=@matricule";
        var employee = await conn.QuerySingleOrDefaultAsync<EmployeeDto>(query, new
        {
            matricule = employeeId
        });

        if (employee is null)
        {
            return null;
        }
        return employee;
    }
}
