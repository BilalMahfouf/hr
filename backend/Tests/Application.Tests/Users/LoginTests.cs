using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Modules.Shared.Abstracions;
using Modules.Shared.Errors;
using Modules.Identity.Abstracions;
using Modules.Identity.Domain.Users;
using Modules.Identity.Application.Users;
using Xunit;

namespace Application.Tests.Users;

public class LoginTests
{
    private readonly Mock<IIdentityApplicationDbContext> _mockDbContext;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtProvider> _mockJwtProvider;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<DbSet<UserSession>> _mockUserSessionDbSet;

    public LoginTests()
    {
        _mockDbContext = new Mock<IIdentityApplicationDbContext>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtProvider = new Mock<IJwtProvider>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockUserSessionDbSet = new Mock<DbSet<UserSession>>();

        _mockDbContext.Setup(db => db.UserSessions).Returns(_mockUserSessionDbSet.Object);
        
        // Setup HttpContext mock
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
    }

    private Login.LoginCommandHandler CreateHandler()
    {
        return new Login.LoginCommandHandler(
            _mockDbContext.Object,
            _mockPasswordHasher.Object,
            _mockJwtProvider.Object,
            _mockHttpContextAccessor.Object);
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
        var command = new Login.LoginCommand("test@example.com", "password123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("User.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Contains("test@example.com", result.Error.Description);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsInvalid_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        var email = "test@example.com";
        var password = "wrongpassword";
        var hashedPassword = "hashedpassword";

        var user = User.Create(
            "John",
            "Doe",
            email,
            hashedPassword,
            UserRoles.Doctor);

        SetupUsersDbSet([user]);

        _mockPasswordHasher
            .Setup(ph => ph.Verify(password, hashedPassword))
            .Returns(false);

        var handler = CreateHandler();
        var command = new Login.LoginCommand(email, password);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("User.InvalidCredentials", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal("The provided credentials are invalid", result.Error.Description);

        _mockPasswordHasher.Verify(ph => ph.Verify(password, hashedPassword), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ShouldReturnSuccessWithToken()
    {
        // Arrange
        var email = "test@example.com";
        var password = "correctpassword";
        var hashedPassword = "hashedpassword";
        var expectedToken = "jwt-token-123";
        var expectedRefreshToken = "refresh-token-456";

        var user = User.Create(
            "John",
            "Doe",
            email,
            hashedPassword,
            UserRoles.Doctor);

        SetupUsersDbSet([user]);

        _mockPasswordHasher
            .Setup(ph => ph.Verify(password, hashedPassword))
            .Returns(true);

        _mockJwtProvider
            .Setup(jp => jp.GenerateToken(It.IsAny<User>()))
            .Returns(expectedToken);

        _mockJwtProvider
            .Setup(jp => jp.GenerateRefreshToken())
            .Returns(expectedRefreshToken);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new Login.LoginCommand(email, password);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedToken, result.Value.Token);

        _mockPasswordHasher.Verify(ph => ph.Verify(password, hashedPassword), Times.Once);
        _mockJwtProvider.Verify(jp => jp.GenerateToken(It.IsAny<User>()), Times.Once);
        _mockJwtProvider.Verify(jp => jp.GenerateRefreshToken(), Times.Once);
        _mockUserSessionDbSet.Verify(db => db.Add(It.IsAny<UserSession>()), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ShouldCreateUserSessionWithCorrectProperties()
    {
        // Arrange
        var email = "test@example.com";
        var password = "correctpassword";
        var hashedPassword = "hashedpassword";
        var expectedToken = "jwt-token-123";
        var expectedRefreshToken = "refresh-token-456";

        var user = User.Create(
            "John",
            "Doe",
            email,
            hashedPassword,
            UserRoles.Admin);

        SetupUsersDbSet([user]);

        _mockPasswordHasher
            .Setup(ph => ph.Verify(password, hashedPassword))
            .Returns(true);

        _mockJwtProvider
            .Setup(jp => jp.GenerateToken(It.IsAny<User>()))
            .Returns(expectedToken);

        _mockJwtProvider
            .Setup(jp => jp.GenerateRefreshToken())
            .Returns(expectedRefreshToken);

        UserSession? capturedUserSession = null;
        _mockUserSessionDbSet
            .Setup(db => db.Add(It.IsAny<UserSession>()))
            .Callback<UserSession>(session => capturedUserSession = session);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new Login.LoginCommand(email, password);
        var beforeExecutionTime = DateTime.UtcNow;

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        var afterExecutionTime = DateTime.UtcNow;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedUserSession);
        Assert.Equal(user.Id, capturedUserSession.UserId);
        Assert.Equal(expectedRefreshToken, capturedUserSession.Token);
        Assert.Equal(UserSessionTokenType.Refresh, capturedUserSession.TokenType);
        Assert.NotNull(capturedUserSession.ExpiresAt);
        Assert.True(capturedUserSession.ExpiresAt >= beforeExecutionTime.AddDays(7));
        Assert.True(capturedUserSession.ExpiresAt <= afterExecutionTime.AddDays(7));
    }

    [Fact]
    public async Task Handle_WithDifferentUserRoles_ShouldSucceedForAdminUser()
    {
        // Arrange
        var email = "admin@example.com";
        var password = "adminpassword";
        var hashedPassword = "hashedadminpassword";

        var user = User.Create(
            "Admin",
            "User",
            email,
            hashedPassword,
            UserRoles.Admin);

        SetupUsersDbSet([user]);

        _mockPasswordHasher
            .Setup(ph => ph.Verify(password, hashedPassword))
            .Returns(true);

        _mockJwtProvider
            .Setup(jp => jp.GenerateToken(It.IsAny<User>()))
            .Returns("admin-token");

        _mockJwtProvider
            .Setup(jp => jp.GenerateRefreshToken())
            .Returns("admin-refresh-token");

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new Login.LoginCommand(email, password);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("admin-token", result.Value.Token);
    }

    [Fact]
    public async Task Handle_WhenCancellationRequested_ShouldPassCancellationTokenToDatabase()
    {
        // Arrange
        var email = "test@example.com";
        var password = "password";
        var hashedPassword = "hashed";

        var user = User.Create("John", "Doe", email, hashedPassword, UserRoles.Doctor);

        SetupUsersDbSet([user]);

        _mockPasswordHasher
            .Setup(ph => ph.Verify(password, hashedPassword))
            .Returns(true);

        _mockJwtProvider.Setup(jp => jp.GenerateToken(It.IsAny<User>())).Returns("token");
        _mockJwtProvider.Setup(jp => jp.GenerateRefreshToken()).Returns("refresh");

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new Login.LoginCommand(email, password);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        _mockDbContext.Verify(db => db.SaveChangesAsync(cancellationToken), Times.Once);
    }
}
