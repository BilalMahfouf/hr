using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Domain.Users;
using VeterinaryApi.Features.Users;
using Xunit;

namespace Application.Tests.Users;

public class GetUserByIdTests
{
    private readonly Mock<IApplicationDbContext> _mockDbContext;

    public GetUserByIdTests()
    {
        _mockDbContext = new Mock<IApplicationDbContext>();
    }

    private GetUserById.GetUserByIdQueryHandler CreateHandler()
    {
        return new GetUserById.GetUserByIdQueryHandler(_mockDbContext.Object);
    }

    private void SetupUsersDbSet(List<User> users)
    {
        var mockUserDbSet = DbSetMockHelper.CreateMockDbSet(users);
        _mockDbContext.Setup(db => db.Users).Returns(mockUserDbSet.Object);
    }

    private void SetupSubscriptionsDbSet(List<Subscription> subscriptions)
    {
        var mockSubDbSet = DbSetMockHelper.CreateMockDbSet(subscriptions);
        _mockDbContext.Setup(db => db.Subscriptions).Returns(mockSubDbSet.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnUserNotFoundError()
    {
        // Arrange
        SetupUsersDbSet([]);
        SetupSubscriptionsDbSet([]);

        var handler = CreateHandler();
        var query = new GetUserById.GetUserByIdQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUserExistsWithoutSubscription_ShouldReturnUserWithNoSubscription()
    {
        // Arrange
        var user = User.Register("user1", "Jane", "Doe", "test@example.com", "hash");
        SetupUsersDbSet([user]);
        SetupSubscriptionsDbSet([]);

        var handler = CreateHandler();
        var query = new GetUserById.GetUserByIdQuery(user.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Null(result.Value.SubscriptionStatus);
        Assert.False(result.Value.IsSubscriptionExist ?? false);
    }

    [Fact]
    public async Task Handle_WhenUserHasSubscriptions_ShouldReturnLatestStatus()
    {
        // Arrange
        var user = User.Register("user1", "Jane", "Doe", "test@example.com", "hash");
        SetupUsersDbSet([user]);

        var plan = SubscriptionPlan.Create("Starter", "starter", Money.InDzd(100), "month");
        var olderSubscription = Subscription.Create(user.Id, plan);
        olderSubscription.MarkPastDue();
        olderSubscription.CreatedOnUtc = DateTime.UtcNow.AddDays(-2);

        var newerSubscription = Subscription.Create(user.Id, plan);
        newerSubscription.Activate();
        newerSubscription.CreatedOnUtc = DateTime.UtcNow.AddDays(-1);

        SetupSubscriptionsDbSet([olderSubscription, newerSubscription]);

        var handler = CreateHandler();
        var query = new GetUserById.GetUserByIdQuery(user.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(SubscriptionStatus.Active.ToString(), result.Value.SubscriptionStatus);
        Assert.True(result.Value.IsSubscriptionExist);
    }
}
