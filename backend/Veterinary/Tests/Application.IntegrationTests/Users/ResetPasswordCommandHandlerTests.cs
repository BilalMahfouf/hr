using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Users;
using VeterinaryApi.Features.Users;
using VeterinaryApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Users;

public sealed class ResetPasswordCommandHandlerTests : UsersTestBase
{
    public ResetPasswordCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        using var scope = CreateScope();
        var handler = CreateResetPasswordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new ResetPassword.ResetPasswordCommand("NewPass123!", "NewPass123!", "token", "missing@test.local"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.UserNotFound("missing@test.local").Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenTokenInvalid_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "user@test.local", password: "Pass1234!");
        var handler = CreateResetPasswordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new ResetPassword.ResetPasswordCommand("NewPass123!", "NewPass123!", "bad-token", user.Email),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.InvalidCredentials.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "user@test.local", password: "Pass1234!");
        var token = "expired-token";
        await SeedResetSessionAsync(db, user, token, DateTime.UtcNow.AddMinutes(-1));
        var handler = CreateResetPasswordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new ResetPassword.ResetPasswordCommand("NewPass123!", "NewPass123!", token, user.Email),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.InvalidCredentials.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenPasswordTooShort_ThrowsDomainException()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "user@test.local", password: "Pass1234!");
        var token = "short-token";
        await SeedResetSessionAsync(db, user, token, DateTime.UtcNow.AddMinutes(10));
        var handler = CreateResetPasswordHandler(scope.ServiceProvider);

        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new ResetPassword.ResetPasswordCommand("123", "123", token, user.Email),
            CancellationToken.None));

        Assert.Equal(UserErrors.InvalidPasswordLength.Code, exception.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenTokenValid_UpdatesPassword()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "user@test.local", password: "Pass1234!");
        var token = "valid-token";
        await SeedResetSessionAsync(db, user, token, DateTime.UtcNow.AddMinutes(10));
        var handler = CreateResetPasswordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new ResetPassword.ResetPasswordCommand("NewPass123!", "NewPass123!", token, user.Email),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await db.Users.SingleAsync(u => u.Id == user.Id);
        var hasher = RootProvider.GetRequiredService<IPasswordHasher>();
        Assert.True(hasher.Verify("NewPass123!", updated.PasswordHash));
    }
}
