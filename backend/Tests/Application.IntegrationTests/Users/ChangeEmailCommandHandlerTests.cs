using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Modules.Identity.Abstracions;
using Modules.Identity.Domain.Users;
using Modules.Identity.Application.Users;

namespace Application.IntegrationTests.Users;

public sealed class ChangeEmailCommandHandlerTests : UsersTestBase
{
    public ChangeEmailCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenEmailInvalid_ThrowsValidationException()
    {
        using var scope = CreateScope();
        var handler = CreateChangeEmailHandler(scope.ServiceProvider);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new ChangeEmail.ChangeEmailCommand("not-an-email"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenEmailInUse_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "user1@test.local");
        var other = await SeedUserAsync(db, email: "user2@test.local");
        SetCurrentTenant(user.Id);
        var handler = CreateChangeEmailHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new ChangeEmail.ChangeEmailCommand(other.Email),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.EmailAlreadyInUse(other.Email).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        using var scope = CreateScope();
        SetCurrentTenant(Guid.NewGuid());
        var handler = CreateChangeEmailHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new ChangeEmail.ChangeEmailCommand("new@test.local"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.UserNotFound("new@test.local").Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesEmail()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "user@test.local");
        SetCurrentTenant(user.Id);
        var handler = CreateChangeEmailHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new ChangeEmail.ChangeEmailCommand("new@test.local"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await db.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Equal("new@test.local", updated.Email);
    }
}
