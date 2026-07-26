using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Common;
using Identity.Domain.Users;
using Identity.Application.Users;
using VeterinaryApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Users;

public sealed class ChangePasswordCommandHandlerTests : UsersTestBase
{
    public ChangePasswordCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        using var scope = CreateScope();
        SetCurrentTenant(Guid.NewGuid());
        var handler = CreateChangePasswordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new ChangePassword.ChangePasswordCommand("Pass1234!", "NewPass123!", "NewPass123!"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordInvalid_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "user@test.local", password: "Pass1234!");
        SetCurrentTenant(user.Id);
        var handler = CreateChangePasswordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new ChangePassword.ChangePasswordCommand("WrongPass!", "NewPass123!", "NewPass123!"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.InvalidPassword.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenNewPasswordTooShort_ThrowsDomainException()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "user@test.local", password: "Pass1234!");
        SetCurrentTenant(user.Id);
        var handler = CreateChangePasswordHandler(scope.ServiceProvider);

        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            new ChangePassword.ChangePasswordCommand("Pass1234!", "123", "123"),
            CancellationToken.None));

        Assert.Equal(UserErrors.InvalidPasswordLength.Code, exception.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesPassword()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "user@test.local", password: "Pass1234!");
        SetCurrentTenant(user.Id);
        var handler = CreateChangePasswordHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new ChangePassword.ChangePasswordCommand("Pass1234!", "NewPass123!", "NewPass123!"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await db.Users.SingleAsync(u => u.Id == user.Id);
        var hasher = RootProvider.GetRequiredService<IPasswordHasher>();
        Assert.True(hasher.Verify("NewPass123!", updated.PasswordHash));
    }
}
