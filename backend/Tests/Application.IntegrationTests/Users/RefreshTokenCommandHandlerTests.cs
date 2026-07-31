using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Modules.Identity.Abstracions;
using Modules.Identity.Domain.Users;
using Modules.Identity.Application.Users;

namespace Application.IntegrationTests.Users;

public sealed class RefreshTokenCommandHandlerTests : UsersTestBase
{
    public RefreshTokenCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenSessionMissing_ReturnsFailure()
    {
        using var scope = CreateScope();
        var handler = CreateRefreshTokenHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new RefreshToken.RefreshTokenCommand("missing"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.InvalidCredentials.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenSessionExpired_ReturnsFailure()
    {
        ResetHttpContext();
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "expired@test.local", password: "Pass1234!");
        var session = await SeedRefreshSessionAsync(
            db,
            user,
            "expiredtoken",
            DateTime.UtcNow.AddMinutes(-1));
        var handler = CreateRefreshTokenHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new RefreshToken.RefreshTokenCommand(session.Token),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.ExpiredRefreshToken.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenSessionValid_RotatesTokenAndSetsCookie()
    {
        ResetHttpContext();
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "valid@test.local", password: "Pass1234!");
        var session = await SeedRefreshSessionAsync(
            db,
            user,
            "oldtoken",
            DateTime.UtcNow.AddDays(1));
        var handler = CreateRefreshTokenHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new RefreshToken.RefreshTokenCommand(session.Token),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));

        var updatedSession = await db.UserSessions.SingleAsync(s => s.UserId == user.Id);
        Assert.NotEqual("oldtoken", updatedSession.Token);

        var cookies = HttpContextAccessor.HttpContext!.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("refreshToken=", cookies);
        Assert.Contains(updatedSession.Token, cookies);
    }
}
