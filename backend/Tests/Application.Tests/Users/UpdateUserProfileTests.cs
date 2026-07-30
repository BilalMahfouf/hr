using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Modules.Identity.Abstracions;
using Modules.Shared.Abstracions;
using Modules.Identity.Domain.Users;
using Modules.Identity.Application.Users;
using Xunit;

namespace Application.Tests.Users;

public class UpdateUserProfileTests
{
    private readonly Mock<IIdentityApplicationDbContext> _mockDbContext;
    private readonly Mock<ICurrentTenant> _mockCurrentTenant;
    private Mock<DbSet<User>>? _mockUserDbSet;

    public UpdateUserProfileTests()
    {
        _mockDbContext = new Mock<IIdentityApplicationDbContext>();
        _mockCurrentTenant = new Mock<ICurrentTenant>();
    }

    private UpdateUserProfile.UpdateUserProfileCommandHandler CreateHandler()
    {
        return new UpdateUserProfile.UpdateUserProfileCommandHandler(
            _mockDbContext.Object,
            _mockCurrentTenant.Object);
    }

    private void SetupUsersDbSet(List<User> users)
    {
        _mockUserDbSet = DbSetMockHelper.CreateMockDbSet(users);
        _mockDbContext.Setup(db => db.Users).Returns(_mockUserDbSet.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockCurrentTenant.SetupGet(t => t.UserId).Returns(userId);
        SetupUsersDbSet([]);

        var handler = CreateHandler();
        var command = new UpdateUserProfile.UpdateUserProfileCommand("newuser", "John", "Smith");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldUpdateProfile()
    {
        // Arrange
        var user = User.Register("olduser", "Jane", "Doe", "test@example.com", "hash");
        SetupUsersDbSet([user]);
        _mockCurrentTenant.SetupGet(t => t.UserId).Returns(user.Id);

        _mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new UpdateUserProfile.UpdateUserProfileCommand("newuser", "John", "Smith");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("newuser", user.UserName);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Smith", user.LastName);
        _mockUserDbSet?.Verify(db => db.Update(user), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
