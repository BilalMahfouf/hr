using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryApi.Common.Paginations.OffSet;
using VeterinaryApi.Domain.Users;
using VeterinaryApi.Features.Users;
using VeterinaryApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Users;

public sealed class GetAllUsersQueryHandlerTests : UsersTestBase
{
    public GetAllUsersQueryHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenNoUsers_ReturnsFailure()
    {
        using var scope = CreateScope();
        var handler = CreateGetAllUsersHandler(scope.ServiceProvider);
        var query = TableRequest<Shared.Response>.Create(null, null);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.UsersNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenSearching_ReturnsMatchingDoctors()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await SeedUserAsync(db, email: "alpha@test.local", userName: "alpha");
        await SeedUserAsync(db, email: "beta@test.local", userName: "beta");
        var handler = CreateGetAllUsersHandler(scope.ServiceProvider);
        var query = TableRequest<Shared.Response>.Create(10, 1, search: "Alpha");

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var items = result.Value.Item.ToList();
        Assert.Single(items);
        Assert.Equal("alpha", items[0].UserName);
    }

    [Fact]
    public async Task Handle_WhenSortingDescending_ReturnsOrderedResults()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await SeedUserAsync(db, email: "alpha@test.local", userName: "alpha");
        await SeedUserAsync(db, email: "beta@test.local", userName: "beta");
        var handler = CreateGetAllUsersHandler(scope.ServiceProvider);
        var query = TableRequest<Shared.Response>.Create(10, 1, sortColumn: "username", sortOrder: "desc");

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var items = result.Value.Item.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("beta", items[0].UserName);
        Assert.Equal("alpha", items[1].UserName);
    }
}
