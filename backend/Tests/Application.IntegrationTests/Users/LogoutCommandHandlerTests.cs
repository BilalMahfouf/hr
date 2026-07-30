using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Modules.Identity.Domain.Users;
using Modules.Identity.Application.Users;
using PublicApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Users;

public sealed class LogoutCommandHandlerTests : UsersTestBase
{
    public LogoutCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenSessionMissing_ReturnsFailure()
    {
        using var scope = CreateScope();
        var handler = CreateLogoutHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new Logout.LogoutCommand("missing"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.InvalidCredentials.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenSessionExists_RemovesSessionAndClearsCookie()
    {
        ResetHttpContext();
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await SeedUserAsync(db);
        var session = await SeedRefreshSessionAsync(db, user, "refresh-token");
        var handler = CreateLogoutHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new Logout.LogoutCommand(session.Token),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var remaining = await db.UserSessions.SingleOrDefaultAsync(s => s.Id == session.Id);
        Assert.Null(remaining);

        var cookies = HttpContextAccessor.HttpContext!.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("refreshToken=", cookies);
    }
}
