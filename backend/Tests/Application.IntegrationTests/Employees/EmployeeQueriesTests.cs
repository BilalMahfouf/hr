using Application.IntegrationTests.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Application.GetEmployeeById;
using Modules.Employees.Application.GetEmployees;
using Modules.Shared.CQRS;
using Modules.Shared;
using Modules.Shared.Paginations.OffSet;

namespace Application.IntegrationTests.Employees;

public sealed class EmployeeQueriesTests : IAsyncLifetime
{
    private readonly MsSqlFixture _fixture = new();
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();

        await using var conn = new SqlConnection(_fixture.ConnectionString);
        await conn.ExecuteAsync("""
            create table dbo.TP_Groupes (
                CodeGrp char(2) not null primary key,
                Designation varchar(50) null);

            create table dbo.T_OrgDepartements (
                Code char(3) not null primary key,
                Designation varchar(100) null);

            create table dbo.T_EmPloyes (
                Matricule char(13) not null primary key,
                Bdg char(5) null,
                Nom char(40) null,
                Prenom char(40) null,
                DateNaiss datetime null,
                LieuNaiss char(40) null,
                NTel char(14) null,
                Sexe char(1) null,
                Adresse char(300) null,
                Nationalite char(2) null,
                CodeGrpP char(2) null,
                CodeDep char(3) null,
                CodeNiv char(2) null,
                Spec char(2) null,
                Photo varbinary(max) null);
            """);

        await conn.ExecuteAsync("""
            insert into dbo.TP_Groupes (CodeGrp, Designation) values
                ('01', 'Grp Surface'),
                ('02', 'Grp Securite');

            insert into dbo.T_OrgDepartements (Code, Designation) values
                ('01', 'DEPARTEMENT PRODUCTION');

            insert into dbo.T_EmPloyes
                (Matricule, Bdg, Nom, Prenom, DateNaiss, LieuNaiss, NTel, Sexe,
                 Adresse, Nationalite, CodeGrpP, CodeDep, CodeNiv, Spec, Photo)
            values
                ('00140C', '140', 'Benali', 'Ali', '1985-04-12', 'Alger',
                 '0550112233', 'M', 'Rue 1', 'DZ', '01', '01', '09', 'ME', 0x01020304),
                ('00205X', '205', 'Cherif', 'Sara', null, 'Oran',
                 null, 'F', null, 'DZ', null, '00', null, null, null),
                ('00300A', '300', 'Dupont', 'Omar', '1990-01-01', 'Blida',
                 '0770000000', 'M', 'Rue 3', 'DZ', '02', '01', null, null, null);
            """);

        var services = new ServiceCollection();
        services.AddSharedModule(typeof(Modules.Employees.DependencyInjection).Assembly);
        services.AddScoped<ISqlConnectionFactory>(_ =>
            new TestSqlConnectionFactory(_fixture.ConnectionString));
        _provider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    private IQueryHandler<GetEmployees.Query, OffSetPagedList<GetEmployees.Response>> ListHandler =>
        _provider.GetRequiredService<IQueryHandler<GetEmployees.Query, OffSetPagedList<GetEmployees.Response>>>();

    private IQueryHandler<GetEmployeeById.Query, GetEmployeeById.Response> DetailsHandler =>
        _provider.GetRequiredService<IQueryHandler<GetEmployeeById.Query, GetEmployeeById.Response>>();

    [Fact]
    public async Task Handle_ReturnsAllEmployees_WithGroupAndDepartmentResolved()
    {
        var result = await ListHandler.Handle(new GetEmployees.Query(1, 10, null, null, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCount);
        var ali = result.Value.Item.Single(e => e.Matricule == "00140C");
        Assert.Equal("140", ali.Bdg);
        Assert.Equal("Ali", ali.FirstName);
        Assert.Equal("Benali", ali.LastName);
        Assert.Equal("Grp Surface", ali.Group);
        Assert.Equal("DEPARTEMENT PRODUCTION", ali.Department);
        Assert.Equal("0550112233", ali.Phone);
    }

    [Fact]
    public async Task Handle_EmployeeWithUnknownDepartmentCode_ReturnsNullDepartment()
    {
        var result = await ListHandler.Handle(new GetEmployees.Query(1, 10, null, null, null));

        var sara = result.Value.Item.Single(e => e.Matricule == "00205X");
        Assert.Null(sara.Department);
        Assert.Null(sara.Group);
    }

    [Fact]
    public async Task Handle_SearchFiltersByLastName()
    {
        var result = await ListHandler.Handle(new GetEmployees.Query(1, 10, "benali", null, null));

        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal("00140C", result.Value.Item.Single().Matricule);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsRequestedPage()
    {
        var result = await ListHandler.Handle(new GetEmployees.Query(1, 2, null, null, null));

        Assert.Equal(2, result.Value.Item.Count());
        Assert.Equal(3, result.Value.TotalCount);
        Assert.True(result.Value.HasNextPage);
    }

    [Fact]
    public async Task Handle_SortLastNameDescending_OrderItems()
    {
        var result = await ListHandler.Handle(new GetEmployees.Query(1, 10, null, "lastName", "desc"));

        Assert.Equal(["Dupont", "Cherif", "Benali"],
            result.Value.Item.Select(e => e.LastName).ToArray());
    }

    [Fact]
    public async Task Handle_DetailsById_ReturnsMappedEmployee()
    {
        var result = await DetailsHandler.Handle(new GetEmployeeById.Query("00140C"));

        Assert.True(result.IsSuccess);
        var employee = result.Value;
        Assert.Equal("00140C", employee.Matricule);
        Assert.Equal("140", employee.Bdg);
        Assert.Equal("Ali", employee.FirstName);
        Assert.Equal("Benali", employee.LastName);
        Assert.Equal(new DateTime(1985, 4, 12), employee.BirthDate);
        Assert.Equal("Alger", employee.BirthPlace);
        Assert.Equal("0550112233", employee.Phone);
        Assert.Equal("M", employee.Sex);
        Assert.Equal("Rue 1", employee.Address);
        Assert.Equal("DZ", employee.Nationality);
        Assert.Equal("Grp Surface", employee.Group);
        Assert.Equal("DEPARTEMENT PRODUCTION", employee.Department);
        Assert.Equal("09", employee.CodeNiv);
        Assert.Equal("ME", employee.Spec);
        Assert.Equal("AQIDBAU=", employee.PhotoBase64);
    }

    [Fact]
    public async Task Handle_DetailsById_UnknownId_ReturnsNotFound()
    {
        var result = await DetailsHandler.Handle(new GetEmployeeById.Query("99999Z"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Employee.NotFound", result.Error.Code);
    }

    private sealed class TestSqlConnectionFactory(string connectionString) : ISqlConnectionFactory
    {
        public SqlConnection CreateConnection() => new(connectionString);
    }
}
