using Dapper;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Application.GetEmployees;
using Modules.Shared.CQRS;
using Modules.Shared.Paginations.OffSet;
using Modules.Shared.Results;

namespace Modules.Employees.EnapPresistance;

internal sealed class GetEmployeesQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetEmployees.Query, OffSetPagedList<GetEmployees.Response>>
{
    private const string _sql = """
        select
            rtrim(e.Matricule)     as Matricule,
            rtrim(e.Bdg)           as Bdg,
            rtrim(e.Prenom)        as FirstName,
            rtrim(e.Nom)           as LastName,
            rtrim(g.Designation)   as [Group],
            rtrim(d.Designation)   as Department,
            rtrim(e.NTel)          as Phone
        from dbo.T_EmPloyes e
        left join dbo.TP_Groupes g on rtrim(g.CodeGrp) = rtrim(e.CodeGrpP)
        left join dbo.T_OrgDepartements d on rtrim(d.Code) = rtrim(e.CodeDep)
        """;

    public async Task<Result<OffSetPagedList<GetEmployees.Response>>> Handle(
        GetEmployees.Query query,
        CancellationToken cancellationToken = default)
    {
        using var conn = sqlConnectionFactory.CreateConnection();
        var rows = (await conn.QueryAsync<EmployeeRow>(
            new CommandDefinition(_sql, cancellationToken: cancellationToken)))
            .AsList();

        var responses = rows.Select(Map).ToList();

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 10 : query.PageSize;
        var search = string.IsNullOrWhiteSpace(query.Search)
            ? null : query.Search.Trim().ToLower();

        if (search is not null)
        {
            responses = responses
                .Where(r =>
                    r.Matricule.ToLower().Contains(search) ||
                    (r.Bdg?.ToLower().Contains(search) ?? false) ||
                    r.FirstName.ToLower().Contains(search) ||
                    r.LastName.ToLower().Contains(search) ||
                    (r.Group?.ToLower().Contains(search) ?? false) ||
                    (r.Department?.ToLower().Contains(search) ?? false) ||
                    (r.Phone?.ToLower().Contains(search) ?? false))
                .ToList();
        }

        var orderBy = query.SortColumn?.Trim().ToLower() switch
        {
            "bdg" => (Func<GetEmployees.Response, object?>)(r => r.Bdg),
            "firstname" => r => r.FirstName,
            "lastname" => r => r.LastName,
            "group" => r => r.Group,
            "department" => r => r.Department,
            "phone" => r => r.Phone,
            _ => r => r.Matricule,
        };

        var ordered = query.SortOrder?.Trim().ToLower() == "desc"
            ? responses.OrderByDescending(orderBy)
            : responses.OrderBy(orderBy);

        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Result<OffSetPagedList<GetEmployees.Response>>.Success(
            OffSetPagedList<GetEmployees.Response>.Create(
                items,
                responses.Count,
                page,
                pageSize));
    }

    private static GetEmployees.Response Map(EmployeeRow row) => new(
        row.Matricule?.Trim() ?? string.Empty,
        row.Bdg?.Trim(),
        row.FirstName?.Trim() ?? string.Empty,
        row.LastName?.Trim() ?? string.Empty,
        row.Group?.Trim(),
        row.Department?.Trim(),
        row.Phone?.Trim());

    private sealed record EmployeeRow(
        string? Matricule,
        string? Bdg,
        string? FirstName,
        string? LastName,
        string? Group,
        string? Department,
        string? Phone);
}
