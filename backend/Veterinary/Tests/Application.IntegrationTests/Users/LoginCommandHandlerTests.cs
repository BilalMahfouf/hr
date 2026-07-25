using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryApi.Domain.Users;
using VeterinaryApi.Features.Users;

namespace Application.IntegrationTests.Users;

public sealed class LoginCommandHandlerTests : UsersTestBase
{
    public LoginCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenCredentialsValid_ReturnsTokenAndSession()
    {
        ResetHttpContext();
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VeterinaryApi.Infrastructure.Persistence.ApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "doctor@test.local", password: "Pass1234!");
        var handler = CreateLoginHandler(scope.ServiceProvider);

        var command = new Login.LoginCommand(user.Email, "Pass1234!");
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));

        var session = await db.UserSessions.SingleOrDefaultAsync(s => s.UserId == user.Id);
        Assert.NotNull(session);
        Assert.Equal(UserSessionTokenType.Refresh, session!.TokenType);

        var cookies = HttpContextAccessor.HttpContext!.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("refreshToken=", cookies);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        using var scope = CreateScope();
        var handler = CreateLoginHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new Login.LoginCommand("missing@test.local", "Pass1234!"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.UserNotFound("missing@test.local").Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenPasswordInvalid_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VeterinaryApi.Infrastructure.Persistence.ApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "doctor@test.local", password: "Pass1234!");
        var handler = CreateLoginHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new Login.LoginCommand(user.Email, "WrongPass!"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.InvalidCredentials.Code, result.Error.Code);
    }
}
