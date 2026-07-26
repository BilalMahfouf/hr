using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Identity.Domain.Users;
using Identity.Application.Users;
using VeterinaryApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Users;

public sealed class ForgetPasswordCommandHandlerTests : UsersTestBase
{
    public ForgetPasswordCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        using var scope = CreateScope();
        var handler = CreateForgetPasswordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new ForgetPassword.ForgetPasswordCommand("missing@test.local", "https://client.test/reset"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.UserNotFound("missing@test.local").Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUserExists_CreatesResetSessionAndOutboxMessage()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "doctor@test.local");
        var handler = CreateForgetPasswordHandler(scope.ServiceProvider);
        var before = DateTime.UtcNow;

        var result = await handler.Handle(
            new ForgetPassword.ForgetPasswordCommand(user.Email, "https://client.test/reset"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var session = await db.UserSessions.SingleOrDefaultAsync(s =>
            s.UserId == user.Id && s.TokenType == UserSessionTokenType.ResetPassword);
        Assert.NotNull(session);
        Assert.NotNull(session!.ExpiresAt);
        Assert.InRange(session.ExpiresAt!.Value, before.AddMinutes(14), before.AddMinutes(16));

        var outbox = await db.OutboxMessages.ToListAsync();
        Assert.Single(outbox);
        Assert.Contains("UserForgetPasswordDomainEvent", outbox[0].Name);
    }
}
