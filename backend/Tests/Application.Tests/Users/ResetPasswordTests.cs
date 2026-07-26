using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Identity.Abstracions;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.Errors;
using Identity.Domain.Users;
using Identity.Application.Users;
using Xunit;

namespace Application.Tests.Users;

public class ResetPasswordTests
{
    private readonly Mock<IIdentityApplicationDbContext> _mockDbContext;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;

    public ResetPasswordTests()
    {
        _mockDbContext = new Mock<IIdentityApplicationDbContext>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
    }

    private ResetPassword.ResetPasswordCommandHandler CreateHandler()
    {
        return new ResetPassword.ResetPasswordCommandHandler(
            _mockDbContext.Object,
            _mockPasswordHasher.Object);
    }

    private void SetupUsersDbSet(List<User> users)
    {
        var mockUserDbSet = DbSetMockHelper.CreateMockDbSet(users);
        _mockDbContext.Setup(db => db.Users).Returns(mockUserDbSet.Object);
    }

    private void SetupUserSessionsDbSet(List<UserSession> sessions)
    {
        var mockUserSessionDbSet = DbSetMockHelper.CreateMockDbSet(sessions);
        _mockDbContext.Setup(db => db.UserSessions).Returns(mockUserSessionDbSet.Object);
    }

    private static User CreateUserWithSessions(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        UserRoles role,
        List<UserSession> sessions)
    {
        var user = User.Create(firstName, lastName, email, passwordHash, role);

        // Use reflection to add sessions to the private _sessions field
        var sessionsField = typeof(User).GetField("_sessions", BindingFlags.NonPublic | BindingFlags.Instance);
        if (sessionsField != null)
        {
            var sessionsList = (List<UserSession>)sessionsField.GetValue(user)!;
            foreach (var session in sessions)
            {
                session.UserId = user.Id;
                sessionsList.Add(session);
            }
        }

        return user;
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnUserNotFoundError()
    {
        // Arrange
        SetupUsersDbSet([]);
        var handler = CreateHandler();
        var command = new ResetPassword.ResetPasswordCommand(
            "newpassword",
            "newpassword",
            "some-token",
            "notfound@example.com");

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
    public async Task Handle_WhenTokenNotFoundInUserSessions_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        var email = "test@example.com";
        var user = CreateUserWithSessions(
            "John",
            "Doe",
            email,
            "hashedpassword",
            UserRoles.Doctor,
            []);

        SetupUsersDbSet([user]);
        SetupUserSessionsDbSet([]);

        var handler = CreateHandler();
        var command = new ResetPassword.ResetPasswordCommand(
            "newpassword",
            "newpassword",
            "non-existent-token",
            email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("User.InvalidCredentials", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        var email = "test@example.com";
        var token = "expired-reset-token";

        var expiredSession = new UserSession
        {
            Token = token,
            TokenType = UserSessionTokenType.ResetPassword,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
            CreatedOnUtc = DateTime.UtcNow.AddMinutes(-16)
        };

        var user = CreateUserWithSessions(
            "John",
            "Doe",
            email,
            "hashedpassword",
            UserRoles.Doctor,
            [expiredSession]);

        SetupUsersDbSet([user]);
        SetupUserSessionsDbSet([expiredSession]);

        var handler = CreateHandler();
        var command = new ResetPassword.ResetPasswordCommand(
            "newpassword",
            "newpassword",
            token,
            email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("User.InvalidCredentials", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenTokenTypeIsNotResetPassword_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        var email = "test@example.com";
        var token = "refresh-token";

        var refreshSession = new UserSession
        {
            Token = token,
            TokenType = UserSessionTokenType.Refresh, // Wrong type
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedOnUtc = DateTime.UtcNow
        };

        var user = CreateUserWithSessions(
            "John",
            "Doe",
            email,
            "hashedpassword",
            UserRoles.Doctor,
            [refreshSession]);

        SetupUsersDbSet([user]);
        SetupUserSessionsDbSet([refreshSession]);

        var handler = CreateHandler();
        var command = new ResetPassword.ResetPasswordCommand(
            "newpassword",
            "newpassword",
            token,
            email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("User.InvalidCredentials", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenValidToken_ShouldResetPasswordSuccessfully()
    {
        // Arrange
        var email = "test@example.com";
        var token = "valid-reset-token";
        var newPassword = "newpassword123";
        var newHashedPassword = "new-hashed-password";

        var validSession = new UserSession
        {
            Token = token,
            TokenType = UserSessionTokenType.ResetPassword,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedOnUtc = DateTime.UtcNow.AddMinutes(-5)
        };

        var user = CreateUserWithSessions(
            "John",
            "Doe",
            email,
            "old-hashed-password",
            UserRoles.Doctor,
            [validSession]);

        SetupUsersDbSet([user]);
        SetupUserSessionsDbSet([validSession]);

        _mockPasswordHasher
            .Setup(ph => ph.Hash(newPassword))
            .Returns(newHashedPassword);

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ResetPassword.ResetPasswordCommand(
            newPassword,
            newPassword,
            token,
            email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newHashedPassword, user.PasswordHash);
        _mockPasswordHasher.Verify(ph => ph.Hash(newPassword), Times.Once);
        _mockDbContext.Verify(db => db.Users.Update(user), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancellationRequested_ShouldPassCancellationTokenToDatabase()
    {
        // Arrange
        var email = "test@example.com";
        var token = "valid-reset-token";

        var validSession = new UserSession
        {
            Token = token,
            TokenType = UserSessionTokenType.ResetPassword,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedOnUtc = DateTime.UtcNow
        };

        var user = CreateUserWithSessions(
            "John",
            "Doe",
            email,
            "hashedpassword",
            UserRoles.Doctor,
            [validSession]);

        SetupUsersDbSet([user]);
        SetupUserSessionsDbSet([validSession]);

        _mockPasswordHasher.Setup(ph => ph.Hash(It.IsAny<string>())).Returns("new-hash");

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ResetPassword.ResetPasswordCommand(
            "newpassword",
            "newpassword",
            token,
            email);

        // Act
        var result = await handler.Handle(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        _mockDbContext.Verify(db => db.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleSessions_ShouldSucceedWithValidResetPasswordSession()
    {
        // Arrange
        var email = "test@example.com";
        var validToken = "valid-reset-token";

        var refreshSession = new UserSession
        {
            Token = "refresh-token",
            TokenType = UserSessionTokenType.Refresh,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedOnUtc = DateTime.UtcNow
        };

        var expiredResetSession = new UserSession
        {
            Token = "expired-reset-token",
            TokenType = UserSessionTokenType.ResetPassword,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10),
            CreatedOnUtc = DateTime.UtcNow.AddMinutes(-25)
        };

        var validResetSession = new UserSession
        {
            Token = validToken,
            TokenType = UserSessionTokenType.ResetPassword,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedOnUtc = DateTime.UtcNow.AddMinutes(-5)
        };

        var user = CreateUserWithSessions(
            "John",
            "Doe",
            email,
            "old-hashed-password",
            UserRoles.Doctor,
            [refreshSession, expiredResetSession, validResetSession]);

        SetupUsersDbSet([user]);
        SetupUserSessionsDbSet([refreshSession, expiredResetSession, validResetSession]);

        _mockPasswordHasher.Setup(ph => ph.Hash(It.IsAny<string>())).Returns("new-hash");

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ResetPassword.ResetPasswordCommand(
            "newpassword",
            "newpassword",
            validToken,
            email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithDifferentUserRoles_ShouldSucceedForAdminUser()
    {
        // Arrange
        var email = "admin@example.com";
        var token = "admin-reset-token";

        var validSession = new UserSession
        {
            Token = token,
            TokenType = UserSessionTokenType.ResetPassword,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedOnUtc = DateTime.UtcNow
        };

        var user = CreateUserWithSessions(
            "Admin",
            "User",
            email,
            "old-admin-hash",
            UserRoles.Admin,
            [validSession]);

        SetupUsersDbSet([user]);
        SetupUserSessionsDbSet([validSession]);

        _mockPasswordHasher.Setup(ph => ph.Hash(It.IsAny<string>())).Returns("new-admin-hash");

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ResetPassword.ResetPasswordCommand(
            "newadminpassword",
            "newadminpassword",
            token,
            email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new-admin-hash", user.PasswordHash);
    }

    [Fact]
    public async Task Handle_WhenTokenJustExpired_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        var email = "test@example.com";
        var token = "just-expired-token";

        var justExpiredSession = new UserSession
        {
            Token = token,
            TokenType = UserSessionTokenType.ResetPassword,
            ExpiresAt = DateTime.UtcNow.AddSeconds(-1), // Just expired
            CreatedOnUtc = DateTime.UtcNow.AddMinutes(-15)
        };

        var user = CreateUserWithSessions(
            "John",
            "Doe",
            email,
            "hashedpassword",
            UserRoles.Doctor,
            [justExpiredSession]);

        SetupUsersDbSet([user]);
        SetupUserSessionsDbSet([justExpiredSession]);

        var handler = CreateHandler();
        var command = new ResetPassword.ResetPasswordCommand(
            "newpassword",
            "newpassword",
            token,
            email);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal("User.InvalidCredentials", result.Error.Code);
    }
}
