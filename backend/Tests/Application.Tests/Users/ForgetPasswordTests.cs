using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Modules.Shared.Abstracions;
using Modules.Shared.Abstracions.Emails;
using Modules.Shared.Errors;
using Modules.Shared.Results;
using Modules.Identity.Abstracions;
using Modules.Identity.Domain.Users;
using Modules.Identity.Application.Users;
using Xunit;

namespace Application.Tests.Users;

public class ForgetPasswordTests
{
    private readonly Mock<IIdentityApplicationDbContext> _mockDbContext;
    private readonly Mock<IJwtProvider> _mockJwtProvider;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<DbSet<UserSession>> _mockUserSessionDbSet;

    public ForgetPasswordTests()
    {
        _mockDbContext = new Mock<IIdentityApplicationDbContext>();
        _mockJwtProvider = new Mock<IJwtProvider>();
        _mockEmailService = new Mock<IEmailService>();
        _mockUserSessionDbSet = new Mock<DbSet<UserSession>>();

        _mockDbContext.Setup(db => db.UserSessions).Returns(_mockUserSessionDbSet.Object);
    }

    private ForgetPassword.ForgetPasswordCommandHandler CreateHandler()
    {
        return new ForgetPassword.ForgetPasswordCommandHandler(
            _mockDbContext.Object,
            _mockJwtProvider.Object,
            _mockEmailService.Object);
    }

    private void SetupUsersDbSet(List<User> users)
    {
        var mockUserDbSet = DbSetMockHelper.CreateMockDbSet(users);
        _mockDbContext.Setup(db => db.Users).Returns(mockUserDbSet.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnUserNotFoundError()
    {
        // Arrange
        SetupUsersDbSet([]);
        var handler = CreateHandler();
        var command = new ForgetPassword.ForgetPasswordCommand("notfound@example.com", "https://example.com/reset");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("User.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Contains("notfound@example.com", result.Error.Description);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldGenerateTokenAndCreateUserSession()
    {
        // Arrange
        var email = "test@example.com";
        var clientUri = "https://example.com/reset";
        var expectedToken = "reset-token-123";

        var user = User.Create(
            "John",
            "Doe",
            email,
            "hashedpassword",
            UserRoles.Doctor);

        SetupUsersDbSet([user]);

        _mockJwtProvider
            .Setup(jp => jp.GenerateToken(It.IsAny<User>()))
            .Returns(expectedToken);

        _mockEmailService
            .Setup(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ForgetPassword.ForgetPasswordCommand(email, clientUri);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockJwtProvider.Verify(jp => jp.GenerateToken(It.IsAny<User>()), Times.Once);
        _mockUserSessionDbSet.Verify(db => db.Add(It.IsAny<UserSession>()), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldCreateUserSessionWithCorrectProperties()
    {
        // Arrange
        var email = "test@example.com";
        var clientUri = "https://example.com/reset";
        var expectedToken = "reset-token-123";

        var user = User.Create(
            "John",
            "Doe",
            email,
            "hashedpassword",
            UserRoles.Doctor);

        SetupUsersDbSet([user]);

        _mockJwtProvider
            .Setup(jp => jp.GenerateToken(It.IsAny<User>()))
            .Returns(expectedToken);

        _mockEmailService
            .Setup(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        UserSession? capturedUserSession = null;
        _mockUserSessionDbSet
            .Setup(db => db.Add(It.IsAny<UserSession>()))
            .Callback<UserSession>(session => capturedUserSession = session);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ForgetPassword.ForgetPasswordCommand(email, clientUri);
        var beforeExecutionTime = DateTime.UtcNow;

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        var afterExecutionTime = DateTime.UtcNow;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedUserSession);
        Assert.Equal(user.Id, capturedUserSession.UserId);
        Assert.Equal(expectedToken, capturedUserSession.Token);
        Assert.Equal(UserSessionTokenType.ResetPassword, capturedUserSession.TokenType);
        Assert.NotNull(capturedUserSession.ExpiresAt);
        Assert.True(capturedUserSession.ExpiresAt >= beforeExecutionTime.AddMinutes(15));
        Assert.True(capturedUserSession.ExpiresAt <= afterExecutionTime.AddMinutes(15));
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldNotSendEmailDirectly()
    {
        // Arrange
        var email = "test@example.com";
        var clientUri = "https://example.com/reset";
        var expectedToken = "reset-token-123";

        var user = User.Create(
            "John",
            "Doe",
            email,
            "hashedpassword",
            UserRoles.Doctor);

        SetupUsersDbSet([user]);

        _mockJwtProvider
            .Setup(jp => jp.GenerateToken(It.IsAny<User>()))
            .Returns(expectedToken);

        _mockEmailService
            .Setup(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ForgetPassword.ForgetPasswordCommand(email, clientUri);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockEmailService.Verify(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCancellationRequested_ShouldPassCancellationTokenToDatabase()
    {
        // Arrange
        var email = "test@example.com";
        var clientUri = "https://example.com/reset";

        var user = User.Create("John", "Doe", email, "hashed", UserRoles.Doctor);

        SetupUsersDbSet([user]);

        _mockJwtProvider.Setup(jp => jp.GenerateToken(It.IsAny<User>())).Returns("token");

        _mockEmailService
            .Setup(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ForgetPassword.ForgetPasswordCommand(email, clientUri);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        _mockDbContext.Verify(db => db.SaveChangesAsync(cancellationToken), Times.Once);
        _mockEmailService.Verify(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDifferentUserRoles_ShouldSucceedForAdminUser()
    {
        // Arrange
        var email = "admin@example.com";
        var clientUri = "https://example.com/reset";

        var user = User.Create(
            "Admin",
            "User",
            email,
            "hashedpassword",
            UserRoles.Admin);

        SetupUsersDbSet([user]);

        _mockJwtProvider.Setup(jp => jp.GenerateToken(It.IsAny<User>())).Returns("admin-token");

        _mockEmailService
            .Setup(es => es.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ForgetPassword.ForgetPasswordCommand(email, clientUri);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockJwtProvider.Verify(jp => jp.GenerateToken(It.IsAny<User>()), Times.Once);
    }
}
