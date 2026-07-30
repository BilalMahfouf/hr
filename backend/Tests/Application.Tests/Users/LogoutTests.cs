using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Modules.Identity.Abstracions;
using Modules.Shared.Abstracions;
using Modules.Identity.Domain.Users;
using Modules.Identity.Application.Users;
using Xunit;

namespace Application.Tests.Users;

public class LogoutTests
{
    private readonly Mock<IIdentityApplicationDbContext> _mockDbContext;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly DefaultHttpContext _httpContext;

    public LogoutTests()
    {
        _mockDbContext = new Mock<IIdentityApplicationDbContext>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);
    }

    private Logout.LogoutCommandHandler CreateHandler()
    {
        return new Logout.LogoutCommandHandler(
            _mockDbContext.Object,
            _mockHttpContextAccessor.Object);
    }

    private Mock<DbSet<UserSession>> SetupUserSessionsDbSet(List<UserSession> sessions)
    {
        var mockDbSet = DbSetMockHelper.CreateMockDbSet(sessions);
        _mockDbContext.Setup(db => db.UserSessions).Returns(mockDbSet.Object);
        return mockDbSet;
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ShouldReturnInvalidCredentialsError()
    {
        // Arrange
        SetupUserSessionsDbSet([]);
        var handler = CreateHandler();
        var command = new Logout.LogoutCommand("missing-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User.InvalidCredentials", result.Error.Code);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSessionExists_ShouldRemoveSessionAndDeleteCookie()
    {
        // Arrange
        var user = User.Create("John", "Doe", "test@example.com", "hash", UserRoles.Doctor);
        var session = new UserSession
        {
            UserId = user.Id,
            User = user,
            Token = "valid-token",
            TokenType = UserSessionTokenType.Refresh
        };

        var mockDbSet = SetupUserSessionsDbSet([session]);

        _mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new Logout.LogoutCommand("valid-token");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        mockDbSet.Verify(db => db.Remove(session), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        var setCookie = _httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("refreshToken=", setCookie);
    }
}
