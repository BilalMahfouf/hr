using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Identity.Abstracions;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Subscriptions;
using Identity.Domain.Users;
using Identity.Application.Users;
using Xunit;

namespace Application.Tests.Users;

public class GetUserByIdTests
{
    private readonly Mock<IIdentityApplicationDbContext> _mockDbContext;
    private readonly Mock<IUserSubscriptionStatusQuery> _mockSubscriptionStatusQuery;

    public GetUserByIdTests()
    {
        _mockDbContext = new Mock<IIdentityApplicationDbContext>();
        _mockSubscriptionStatusQuery = new Mock<IUserSubscriptionStatusQuery>();
    }

    private GetUserById.GetUserByIdQueryHandler CreateHandler()
    {
        return new GetUserById.GetUserByIdQueryHandler(_mockDbContext.Object, _mockSubscriptionStatusQuery.Object);
    }

    private void SetupUsersDbSet(List<User> users)
    {
        var mockUserDbSet = DbSetMockHelper.CreateMockDbSet(users);
        _mockDbContext.Setup(db => db.Users).Returns(mockUserDbSet.Object);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnUserNotFoundError()
    {
        // Arrange
        SetupUsersDbSet([]);
        _mockSubscriptionStatusQuery
            .Setup(q => q.GetSubscriptionStatusAsync(It.IsAny<Guid>()))
            .ReturnsAsync((null, false));

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
        _mockSubscriptionStatusQuery
            .Setup(q => q.GetSubscriptionStatusAsync(user.Id))
            .ReturnsAsync((null, false));

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
        _mockSubscriptionStatusQuery
            .Setup(q => q.GetSubscriptionStatusAsync(user.Id))
            .ReturnsAsync((SubscriptionStatus.Active.ToString(), true));

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
