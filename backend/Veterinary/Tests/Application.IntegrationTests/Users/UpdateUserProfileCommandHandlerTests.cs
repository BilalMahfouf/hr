using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryApi.Domain.Users;
using VeterinaryApi.Features.Users;
using VeterinaryApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Users;

public sealed class UpdateUserProfileCommandHandlerTests : UsersTestBase
{
    public UpdateUserProfileCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        using var scope = CreateScope();
        SetCurrentTenant(Guid.NewGuid());
        var handler = CreateUpdateUserProfileHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new UpdateUserProfile.UpdateUserProfileCommand("user", "First", "Last"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesProfile()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "user@test.local", userName: "olduser");
        SetCurrentTenant(user.Id);
        var handler = CreateUpdateUserProfileHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new UpdateUserProfile.UpdateUserProfileCommand("newuser", "New", "Name"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await db.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Equal("newuser", updated.UserName);
        Assert.Equal("New", updated.FirstName);
        Assert.Equal("Name", updated.LastName);
    }
}
