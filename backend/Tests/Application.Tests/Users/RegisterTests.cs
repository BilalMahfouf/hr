using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Modules.Shared.Abstracions;
using Modules.Identity.Abstracions;
using Modules.Identity.Domain.Users;
using Modules.Identity.Application.Users;
using Xunit;

namespace Application.Tests.Users;

public class RegisterTests
{
    private readonly Mock<IIdentityApplicationDbContext> _mockDbContext;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtProvider> _mockJwtProvider;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<DbSet<UserSession>> _mockUserSessionDbSet;
    private readonly DefaultHttpContext _httpContext;
    private Mock<DbSet<User>>? _mockUserDbSet;

    public RegisterTests()
    {
        _mockDbContext = new Mock<IIdentityApplicationDbContext>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtProvider = new Mock<IJwtProvider>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockUserSessionDbSet = new Mock<DbSet<UserSession>>();

        _httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);

        _mockDbContext.Setup(db => db.UserSessions).Returns(_mockUserSessionDbSet.Object);
    }

    private Register.RegisterCommandHandler CreateHandler()
    {
        return new Register.RegisterCommandHandler(
            _mockDbContext.Object,
            _mockPasswordHasher.Object,
            _mockJwtProvider.Object,
            _mockHttpContextAccessor.Object);
    }

    private void SetupUsersDbSet(List<User> users)
    {
        _mockUserDbSet = DbSetMockHelper.CreateMockDbSet(users);
        _mockDbContext.Setup(db => db.Users).Returns(_mockUserDbSet.Object);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyInUse_ShouldReturnEmailAlreadyInUseError()
    {
        // Arrange
        var existingUser = User.Register("user1", "Jane", "Doe", "test@example.com", "hash");
        SetupUsersDbSet([existingUser]);

        _mockPasswordHasher.Setup(ph => ph.Hash(It.IsAny<string>())).Returns("hash");

        var handler = CreateHandler();
        var command = new Register.RegisterCommand(
            "test@example.com",
            "password123",
            "newuser",
            "John",
            "Smith");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User.EmailAlreadyInUse", result.Error.Code);
        _mockUserDbSet?.Verify(db => db.Add(It.IsAny<User>()), Times.Never);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailIsUnique_ShouldCreateUserAndSession()
    {
        // Arrange
        SetupUsersDbSet([]);

        var command = new Register.RegisterCommand(
            "new@example.com",
            "password123",
            "newuser",
            "John",
            "Smith");

        _mockPasswordHasher.Setup(ph => ph.Hash(command.Password)).Returns("hashed-password");
        _mockJwtProvider.Setup(jp => jp.GenerateToken(It.IsAny<User>())).Returns("jwt-token");
        _mockJwtProvider.Setup(jp => jp.GenerateRefreshToken()).Returns("refresh-token");

        _mockDbContext
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        User? capturedUser = null;
        _mockUserDbSet!.Setup(db => db.Add(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u);

        UserSession? capturedSession = null;
        _mockUserSessionDbSet.Setup(db => db.Add(It.IsAny<UserSession>()))
            .Callback<UserSession>(s => capturedSession = s);

        var handler = CreateHandler();
        var beforeExecution = DateTime.UtcNow;

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        var afterExecution = DateTime.UtcNow;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-token", result.Value.Token);

        Assert.NotNull(capturedUser);
        Assert.Equal(command.Email, capturedUser.Email);
        Assert.Equal(command.UserName, capturedUser.UserName);
        Assert.Equal(UserRoles.Doctor, capturedUser.Role);
        Assert.Equal("hashed-password", capturedUser.PasswordHash);

        Assert.NotNull(capturedSession);
        Assert.Equal(capturedUser.Id, capturedSession.UserId);
        Assert.Equal("refresh-token", capturedSession.Token);
        Assert.Equal(UserSessionTokenType.Refresh, capturedSession.TokenType);
        Assert.NotNull(capturedSession.ExpiresAt);
        Assert.True(capturedSession.ExpiresAt >= beforeExecution.AddDays(7));
        Assert.True(capturedSession.ExpiresAt <= afterExecution.AddDays(7));

        var setCookie = _httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("refreshToken=", setCookie);

        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
