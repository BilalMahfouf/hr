using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Tests.Helpers;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
using Identity.Abstracions;
using Shared.Abstracions;
using Identity.Domain.Users;
using Identity.Application.Users;
using Xunit;

namespace Application.Tests.Users;

public class ChangeEmailTests
{
    private readonly Mock<IIdentityApplicationDbContext> _mockDbContext;
    private readonly Mock<ICurrentTenant> _mockCurrentTenant;
    private readonly IValidator<ChangeEmail.ChangeEmailCommand> _validator;
    private Mock<DbSet<User>>? _mockUserDbSet;

    public ChangeEmailTests()
    {
_mockDbContext = new Mock<IIdentityApplicationDbContext>();


        _mockCurrentTenant = new Mock<ICurrentTenant>();
        _validator = new ChangeEmail.Validator();
    }

    private ChangeEmail.ChangeEmailCommandHandler CreateHandler()
    {
        return new ChangeEmail.ChangeEmailCommandHandler(
            _mockDbContext.Object,
            _mockCurrentTenant.Object,
            _validator);
    }

    private void SetupUsersDbSet(List<User> users)
    {
        _mockUserDbSet = DbSetMockHelper.CreateMockDbSet(users);
        _mockDbContext.Setup(db => db.Users).Returns(_mockUserDbSet.Object);
    }

    [Fact]
    public async Task Handle_WhenEmailIsInvalid_ShouldThrowValidationException()
    {
        // Arrange
        SetupUsersDbSet([]);
        _mockCurrentTenant.SetupGet(t => t.UserId).Returns(Guid.NewGuid());

        var handler = CreateHandler();
        var command = new ChangeEmail.ChangeEmailCommand("not-an-email");

        // Act
        var exception = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ValidationException>(exception);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyInUse_ShouldReturnEmailAlreadyInUseError()
    {
        // Arrange
        var existingUser = User.Register("user1", "Jane", "Doe", "used@example.com", "hash");
        SetupUsersDbSet([existingUser]);
        _mockCurrentTenant.SetupGet(t => t.UserId).Returns(Guid.NewGuid());

        var handler = CreateHandler();
        var command = new ChangeEmail.ChangeEmailCommand("used@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User.EmailAlreadyInUse", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnUserNotFoundError()
    {
        // Arrange
        SetupUsersDbSet([]);
        var userId = Guid.NewGuid();
        _mockCurrentTenant.SetupGet(t => t.UserId).Returns(userId);

        var handler = CreateHandler();
        var command = new ChangeEmail.ChangeEmailCommand("new@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldUpdateEmail()
    {
        // Arrange
        var user = User.Register("user1", "Jane", "Doe", "old@example.com", "hash");
        SetupUsersDbSet([user]);
        _mockCurrentTenant.SetupGet(t => t.UserId).Returns(user.Id);

        _mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ChangeEmail.ChangeEmailCommand("new@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new@example.com", user.Email);
        _mockUserDbSet?.Verify(db => db.Update(user), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
