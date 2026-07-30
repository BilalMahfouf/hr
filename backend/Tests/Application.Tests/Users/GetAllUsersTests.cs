using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Modules.Identity.Abstracions;
using Modules.Shared.Abstracions;
using Modules.Shared.Paginations.OffSet;
using Modules.Identity.Domain.Users;
using Modules.Identity.Application.Users;
using Xunit;

namespace Application.Tests.Users;

public class GetAllUsersTests
{
    private readonly Mock<IIdentityApplicationDbContext> _mockDbContext;
    private Mock<DbSet<User>>? _mockUserDbSet;

    public GetAllUsersTests()
    {
        _mockDbContext = new Mock<IIdentityApplicationDbContext>();
    }

    private GetAllUsers.QueryHandler CreateHandler()
    {
        return new GetAllUsers.QueryHandler(_mockDbContext.Object);
    }

    private void SetupUsersDbSet(List<User> users)
    {
        _mockUserDbSet = DbSetMockHelper.CreateMockDbSet(users);
        _mockDbContext.Setup(db => db.Users).Returns(_mockUserDbSet.Object);
    }

    [Fact]
    public async Task Handle_WhenNoUsers_ShouldReturnUsersNotFoundError()
    {
        // Arrange
        SetupUsersDbSet([]);
        var handler = CreateHandler();
        var query = TableRequest<Modules.Identity.Application.Users.Shared.Response>.Create(10, 1);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User.UsersNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUsersExist_ShouldReturnOnlyDoctorsSortedByUserNameDesc()
    {
        // Arrange
        var doctor1 = User.Register("alpha", "Anna", "Zeal", "alpha@example.com", "hash");
        var doctor2 = User.Register("beta", "Bob", "Young", "beta@example.com", "hash");
        var admin = User.Create("Admin", "User", "admin@example.com", "hash", UserRoles.Admin);
        admin.UpdateProfile("gamma", "Admin", "User");

        SetupUsersDbSet([doctor1, doctor2, admin]);

        var handler = CreateHandler();
        var query = TableRequest<Modules.Identity.Application.Users.Shared.Response>.Create(10, 1, null, "username", "desc");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.Item.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("beta", items[0].UserName);
        Assert.Equal("alpha", items[1].UserName);
        Assert.DoesNotContain(items, i => i.Role == UserRoles.Admin.ToString());
    }

    [Fact]
    public async Task Handle_WhenSearchIsProvided_ShouldFilterResults()
    {
        // Arrange
        var doctor1 = User.Register("alpha", "Anna", "Zeal", "alpha@example.com", "hash");
        var doctor2 = User.Register("beta", "Bob", "Young", "beta@example.com", "hash");

        SetupUsersDbSet([doctor1, doctor2]);

        var handler = CreateHandler();
        var query = TableRequest<Modules.Identity.Application.Users.Shared.Response>.Create(10, 1, "ann", "username", "asc");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var items = result.Value.Item.ToList();
        Assert.Single(items);
        Assert.Equal("alpha", items[0].UserName);
    }
}
