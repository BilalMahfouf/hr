using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Users;
using VeterinaryApi.Features.Users;
using Xunit;

namespace Application.Tests.Users;

public class ChangePasswordTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<ICurrentTenant> _mockCurrentTenant;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private Mock<DbSet<User>>? _mockUserDbSet;

    public ChangePasswordTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockCurrentTenant = new Mock<ICurrentTenant>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
    }

    private ChangePassword.ChangePasswordCommandHandler CreateHandler()
    {
        return new ChangePassword.ChangePasswordCommandHandler(
            _mockDbContext.Object,
            _mockCurrentTenant.Object,
            _mockPasswordHasher.Object);
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
        var command = new ChangePassword.ChangePasswordCommand(
            "oldpassword",
            "newpassword",
            "newpassword");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordInvalid_ShouldReturnInvalidPasswordError()
    {
        // Arrange
        var user = User.Create("John", "Doe", "test@example.com", "old-hash", UserRoles.Doctor);
        _mockCurrentTenant.SetupGet(t => t.UserId).Returns(user.Id);
        SetupUsersDbSet([user]);

        _mockPasswordHasher.Setup(ph => ph.Verify("wrong", user.PasswordHash)).Returns(false);

        var handler = CreateHandler();
        var command = new ChangePassword.ChangePasswordCommand(
            "wrong",
            "newpassword",
            "newpassword");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User.InvalidPassword", result.Error.Code);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValidPassword_ShouldUpdatePassword()
    {
        // Arrange
        var user = User.Create("John", "Doe", "test@example.com", "old-hash", UserRoles.Doctor);
        _mockCurrentTenant.SetupGet(t => t.UserId).Returns(user.Id);
        SetupUsersDbSet([user]);

        _mockPasswordHasher.Setup(ph => ph.Verify("oldpassword", user.PasswordHash)).Returns(true);
        _mockPasswordHasher.Setup(ph => ph.Hash("newpassword")).Returns("new-hash");

        _mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ChangePassword.ChangePasswordCommand(
            "oldpassword",
            "newpassword",
            "newpassword");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new-hash", user.PasswordHash);
        _mockUserDbSet?.Verify(db => db.Update(user), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNewPasswordTooShort_ShouldThrowDomainException()
    {
        // Arrange
        var user = User.Create("John", "Doe", "test@example.com", "old-hash", UserRoles.Doctor);
        _mockCurrentTenant.SetupGet(t => t.UserId).Returns(user.Id);
        SetupUsersDbSet([user]);

        _mockPasswordHasher.Setup(ph => ph.Verify("oldpassword", user.PasswordHash)).Returns(true);
        _mockPasswordHasher.Setup(ph => ph.Hash("short")).Returns("short-hash");

        var handler = CreateHandler();
        var command = new ChangePassword.ChangePasswordCommand(
            "oldpassword",
            "short",
            "short");

        // Act
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<DomainException>(exception);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
