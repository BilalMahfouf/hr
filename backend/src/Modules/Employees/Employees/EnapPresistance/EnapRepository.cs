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

    private static string BuildFullName(string? fullName)
        => string.IsNullOrWhiteSpace(fullName) ? string.Empty : fullName.Trim();

    public async Task<EmployeeDto?> GetEmployeeByBgdeAsync(string bdge, CancellationToken ct = default)
    {
        using var conn = sqlConnectionFactory.CreateConnection();
        var query = @$"select Matricule as {nameof(EmployeeDto.EmployeeId)},
        Bdg as {nameof(EmployeeDto.Bdge)},
        CodeGrpP as {nameof(EmployeeDto.EmployeeGroup)},
        (Nom + ' ' + Prenom) as {nameof(EmployeeDto.FullName)}
        from {_employeeTableName} where Bdg=@bdg";

        var employee = await conn.QuerySingleOrDefaultAsync<EmployeeDto>(query, new
        {
            bdg=bdge
        });

        if (employee is null)
        {
            return null;
        }

        return new EmployeeDto(
            employee.EmployeeId.Trim(),
            employee.Bdge.Trim(),
            employee.EmployeeGroup,
            BuildFullName(employee.FullName));
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(string employeeId, CancellationToken ct = default)
    {
        using var conn = sqlConnectionFactory.CreateConnection();
        var query = @$"select Matricule as {nameof(EmployeeDto.EmployeeId)},
        Bdg as {nameof(EmployeeDto.Bdge)},
        CodeGrpP as {nameof(EmployeeDto.EmployeeGroup)},
        (Nom + ' ' + Prenom) as {nameof(EmployeeDto.FullName)}
        from {_employeeTableName} where Matricule=@matricule";

        var employee = await conn.QuerySingleOrDefaultAsync<EmployeeDto>(query, new
        {
            matricule = employeeId
        });

        if (employee is null)
        {
            return null;
        }
        return new EmployeeDto(
            employee.EmployeeId.Trim(),
            employee.Bdge.Trim(),
            employee.EmployeeGroup,
            BuildFullName(employee.FullName));
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesByBgdesAsync(
        IEnumerable<int> bdges,
        CancellationToken ct = default)
    {
        using var conn = sqlConnectionFactory.CreateConnection();
        var query = @$"select Matricule as {nameof(EmployeeDto.EmployeeId)},
        Bdg as {nameof(EmployeeDto.Bdge)},
        CodeGrpP as {nameof(EmployeeDto.EmployeeGroup)},
        (Nom + ' ' + Prenom) as {nameof(EmployeeDto.FullName)}
        from {_employeeTableName} where Bdg in @bdges";

        var employees = await conn.QueryAsync<EmployeeDto>(query, new
        {
            bdges = bdges.ToArray()
        });

        return employees
            .Select(e => new EmployeeDto(
                e.EmployeeId.Trim(),
                e.Bdge.Trim(),
                e.EmployeeGroup,
                BuildFullName(e.FullName)))
            .ToList();
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesByIdsAsync(
        IEnumerable<string> ids,
        CancellationToken ct = default)
    {
        using var conn = sqlConnectionFactory.CreateConnection();
        var query = @$"select Matricule as {nameof(EmployeeDto.EmployeeId)},
        Bdg as {nameof(EmployeeDto.Bdge)},
        CodeGrpP as {nameof(EmployeeDto.EmployeeGroup)},
        (Nom + ' ' + Prenom) as {nameof(EmployeeDto.FullName)}
        from {_employeeTableName} where Matricule in @ids";

        var employees = await conn.QueryAsync<EmployeeDto>(query, new
        {
            ids = ids.ToArray()
        });

        return employees
            .Select(e => new EmployeeDto(
                e.EmployeeId.Trim(),
                e.Bdge.Trim(),
                e.EmployeeGroup,
                BuildFullName(e.FullName)))
            .ToList();
    }
}
