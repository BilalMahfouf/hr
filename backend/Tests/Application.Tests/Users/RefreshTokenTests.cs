using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Abstracions;
using Shared.Errors;
using Identity.Abstracions;
using Identity.Domain.Users;
using Identity.Application.Users;
using Xunit;

namespace Application.Tests.Users;

public class RefreshTokenTests
{
    private readonly Mock<IIdentityApplicationDbContext> _mockDbContext;
    private readonly Mock<IJwtProvider> _mockJwtProvider;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

    public RefreshTokenTests()
    {
        _mockDbContext = new Mock<IIdentityApplicationDbContext>();
        _mockJwtProvider = new Mock<IJwtProvider>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        // Setup HttpContext mock
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
    }

    private RefreshToken.RefreshTokenCommandHandler CreateHandler()
    {
        return new RefreshToken.RefreshTokenCommandHandler(
            _mockDbContext.Object,
            _mockJwtProvider.Object,
            _mockHttpContextAccessor.Object);
    }

    private void SetupUserSessionsDbSet(List<UserSession> sessions)
    {
        var mockUserSessionDbSet = DbSetMockHelper.CreateMockDbSet(sessions);
        _mockDbContext.Setup(db => db.UserSessions).Returns(mockUserSessionDbSet.Object);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        SetupUserSessionsDbSet([]);
        var handler = CreateHandler();
        var command = new RefreshToken.RefreshTokenCommand("non-existent-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("User.InvalidCredentials", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenExpired_ShouldReturnExpiredRefreshTokenError()
    {
        // Arrange
        var user = User.Create(
            "John",
            "Doe",
            "test@example.com",
            "hashedpassword",
            UserRoles.Doctor);

        var expiredSession = new UserSession
        {
            UserId = user.Id,
            Token = "expired-refresh-token",
            TokenType = UserSessionTokenType.Refresh,
            ExpiresAt = DateTime.Now.AddDays(-1), // Expired
            CreatedOnUtc = DateTime.UtcNow.AddDays(-8),
            User = user
        };

        SetupUserSessionsDbSet([expiredSession]);

        var handler = CreateHandler();
        var command = new RefreshToken.RefreshTokenCommand("expired-refresh-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("User.ExpiredRefreshToken", result.Error.Code);
        Assert.Equal("Refresh Token is expired, please login again", result.Error.Description);
    }

    [Fact]
    public async Task Handle_WhenValidRefreshToken_ShouldReturnSuccessWithNewToken()
    {
        // Arrange
        var expectedNewToken = "new-jwt-token";
        var expectedNewRefreshToken = "new-refresh-token";

        var user = User.Create(
            "John",
            "Doe",
            "test@example.com",
            "hashedpassword",
            UserRoles.Doctor);

        var validSession = new UserSession
        {
            UserId = user.Id,
            Token = "valid-refresh-token",
            TokenType = UserSessionTokenType.Refresh,
            ExpiresAt = DateTime.Now.AddDays(7), // Not expired
            CreatedOnUtc = DateTime.UtcNow,
            User = user
        };

        SetupUserSessionsDbSet([validSession]);

        _mockJwtProvider
            .Setup(jp => jp.GenerateToken(It.IsAny<User>()))
            .Returns(expectedNewToken);

        _mockJwtProvider
            .Setup(jp => jp.GenerateRefreshToken())
            .Returns(expectedNewRefreshToken);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new RefreshToken.RefreshTokenCommand("valid-refresh-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(expectedNewToken, result.Value.Token);

        _mockJwtProvider.Verify(jp => jp.GenerateToken(It.IsAny<User>()), Times.Once);
        _mockJwtProvider.Verify(jp => jp.GenerateRefreshToken(), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValidRefreshToken_ShouldUpdateSessionWithNewRefreshToken()
    {
        // Arrange
        var expectedNewRefreshToken = "new-refresh-token";

        var user = User.Create(
            "John",
            "Doe",
            "test@example.com",
            "hashedpassword",
            UserRoles.Doctor);

        var validSession = new UserSession
        {
            UserId = user.Id,
            Token = "valid-refresh-token",
            TokenType = UserSessionTokenType.Refresh,
            ExpiresAt = DateTime.Now.AddDays(7),
            CreatedOnUtc = DateTime.UtcNow,
            User = user
        };

        SetupUserSessionsDbSet([validSession]);

        _mockJwtProvider
            .Setup(jp => jp.GenerateToken(It.IsAny<User>()))
            .Returns("new-jwt-token");

        _mockJwtProvider
            .Setup(jp => jp.GenerateRefreshToken())
            .Returns(expectedNewRefreshToken);

        _mockDbContext.Setup(db => db.UserSessions.Update(It.IsAny<UserSession>()));

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new RefreshToken.RefreshTokenCommand("valid-refresh-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedNewRefreshToken, validSession.Token);
        _mockDbContext.Verify(db => db.UserSessions.Update(It.IsAny<UserSession>()), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentUserRoles_ShouldSucceedForAdminUser()
    {
        // Arrange
        var user = User.Create(
            "Admin",
            "User",
            "admin@example.com",
            "hashedpassword",
            UserRoles.Admin);

        var validSession = new UserSession
        {
            UserId = user.Id,
            Token = "admin-refresh-token",
            TokenType = UserSessionTokenType.Refresh,
            ExpiresAt = DateTime.Now.AddDays(7),
            CreatedOnUtc = DateTime.UtcNow,
            User = user
        };

        SetupUserSessionsDbSet([validSession]);

        _mockJwtProvider
            .Setup(jp => jp.GenerateToken(It.IsAny<User>()))
            .Returns("admin-new-token");

        _mockJwtProvider
            .Setup(jp => jp.GenerateRefreshToken())
            .Returns("admin-new-refresh-token");

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new RefreshToken.RefreshTokenCommand("admin-refresh-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("admin-new-token", result.Value.Token);
    }

    [Fact]
    public async Task Handle_WhenTokenExpiresExactlyNow_ShouldReturnExpiredRefreshTokenError()
    {
        // Arrange
        var user = User.Create(
            "John",
            "Doe",
            "test@example.com",
            "hashedpassword",
            UserRoles.Doctor);

        var sessionExpiringNow = new UserSession
        {
            UserId = user.Id,
            Token = "expiring-now-token",
            TokenType = UserSessionTokenType.Refresh,
            ExpiresAt = DateTime.Now.AddSeconds(-1), // Just expired
            CreatedOnUtc = DateTime.UtcNow.AddDays(-7),
            User = user
        };

        SetupUserSessionsDbSet([sessionExpiringNow]);

        var handler = CreateHandler();
        var command = new RefreshToken.RefreshTokenCommand("expiring-now-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("User.ExpiredRefreshToken", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenMultipleSessionsExist_ShouldFindCorrectSession()
    {
        // Arrange
        var user1 = User.Create("User1", "Test", "user1@example.com", "hash1", UserRoles.Doctor);
        var user2 = User.Create("User2", "Test", "user2@example.com", "hash2", UserRoles.Admin);

        var session1 = new UserSession
        {
            UserId = user1.Id,
            Token = "token-user1",
            TokenType = UserSessionTokenType.Refresh,
            ExpiresAt = DateTime.Now.AddDays(7),
            CreatedOnUtc = DateTime.UtcNow,
            User = user1
        };

        var session2 = new UserSession
        {
            UserId = user2.Id,
            Token = "token-user2",
            TokenType = UserSessionTokenType.Refresh,
            ExpiresAt = DateTime.Now.AddDays(7),
            CreatedOnUtc = DateTime.UtcNow,
            User = user2
        };

        SetupUserSessionsDbSet([session1, session2]);

        _mockJwtProvider
            .Setup(jp => jp.GenerateToken(It.Is<User>(u => u.Email == "user2@example.com")))
            .Returns("user2-new-token");

        _mockJwtProvider
            .Setup(jp => jp.GenerateRefreshToken())
            .Returns("user2-new-refresh-token");

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new RefreshToken.RefreshTokenCommand("token-user2");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("user2-new-token", result.Value.Token);
    }
}
