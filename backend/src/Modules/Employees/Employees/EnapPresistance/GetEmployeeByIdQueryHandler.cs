using Dapper;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Application.GetEmployeeById;
using Modules.Employees.Contracts;
using Modules.Shared.CQRS;
using Modules.Shared.Results;

namespace Modules.Employees.EnapPresistance;

internal sealed class GetEmployeeByIdQueryHandler(ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetEmployeeById.Query, GetEmployeeById.Response>
{
    private const string _sql = """
        select top 1
            rtrim(e.Matricule)     as Matricule,
            rtrim(e.Bdg)           as Bdg,
            rtrim(e.Prenom)        as FirstName,
            rtrim(e.Nom)           as LastName,
            e.DateNaiss            as BirthDate,
            rtrim(e.LieuNaiss)     as BirthPlace,
            rtrim(e.NTel)          as Phone,
            rtrim(e.Sexe)          as Sex,
            rtrim(e.Adresse)       as Address,
            rtrim(e.Nationalite)   as Nationality,
            rtrim(g.Designation)   as [Group],
            rtrim(d.Designation)   as Department,
            rtrim(e.CodeNiv)       as CodeNiv,
            rtrim(e.Spec)          as Spec,
            e.Photo                as Photo
        from dbo.T_EmPloyes e
        left join dbo.TP_Groupes g on rtrim(g.CodeGrp) = rtrim(e.CodeGrpP)
        left join dbo.T_OrgDepartements d on rtrim(d.Code) = rtrim(e.CodeDep)
        where rtrim(e.Matricule) = @id
        """;

    public async Task<Result<GetEmployeeById.Response>> Handle(
        GetEmployeeById.Query query,
        CancellationToken cancellationToken = default)
    {
        using var conn = sqlConnectionFactory.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<EmployeeRow>(
            new CommandDefinition(_sql, new { id = query.Id.Trim() }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result<GetEmployeeById.Response>.Failure(EmployeeErrors.NotFound);
        }

        return Result<GetEmployeeById.Response>.Success(new GetEmployeeById.Response(
            row.Matricule?.Trim() ?? string.Empty,
            row.Bdg?.Trim(),
            row.FirstName?.Trim() ?? string.Empty,
            row.LastName?.Trim() ?? string.Empty,
            row.BirthDate,
            row.BirthPlace?.Trim(),
            row.Phone?.Trim(),
            row.Sex?.Trim(),
            row.Address?.Trim(),
            row.Nationality?.Trim(),
            row.Group?.Trim(),
            row.Department?.Trim(),
            row.CodeNiv?.Trim(),
            row.Spec?.Trim(),
            row.Photo is null ? null : Convert.ToBase64String(row.Photo)));
    }

    private sealed record EmployeeRow(
        string? Matricule,
        string? Bdg,
        string? FirstName,
        string? LastName,
        DateTime? BirthDate,
        string? BirthPlace,
        string? Phone,
        string? Sex,
        string? Address,
        string? Nationality,
        string? Group,
        string? Department,
        string? CodeNiv,
        string? Spec,
        byte[]? Photo);
}
