using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Modules.Identity.Abstracions;
using Modules.Identity.Domain.Users;
using Modules.Identity.Application.Users;

namespace Application.IntegrationTests.Users;

public sealed class RegisterCommandHandlerTests : UsersTestBase
{
    public RegisterCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenEmailIsUnique_CreatesUserSessionAndCookie()
    {
        ResetHttpContext();
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var handler = CreateRegisterHandler(scope.ServiceProvider);

        var command = new Register.RegisterCommand(
            "doctor@test.local",
            "Pass1234!",
            "doctor",
            "Doc",
            "Tor");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));

        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == command.Email);
        Assert.NotNull(user);

        var session = await db.UserSessions.SingleOrDefaultAsync(s => s.UserId == user!.Id);
        Assert.NotNull(session);
        Assert.Equal(UserSessionTokenType.Refresh, session!.TokenType);

        var cookies = HttpContextAccessor.HttpContext!.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("refreshToken=", cookies);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var existing = await SeedUserAsync(db, email: "doctor@test.local");
        var handler = CreateRegisterHandler(scope.ServiceProvider);

        var command = new Register.RegisterCommand(
            existing.Email,
            "Pass1234!",
            "doctor",
            "Doc",
            "Tor");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.EmailAlreadyInUse(existing.Email).Code, result.Error.Code);
    }
}
