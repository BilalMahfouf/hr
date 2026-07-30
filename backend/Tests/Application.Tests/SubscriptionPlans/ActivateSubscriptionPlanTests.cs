using Application.Tests.Helpers;
using Moq;
using Modules.Shared.Abstracions;
using PublicApi.Common.Abstracions;
using Modules.Shared.Domain.Common;
using PublicApi.Domain.Common;
using PublicApi.Domain.Subscriptions;
using PublicApi.Features.SubscriptionPlans;

namespace Application.Tests.SubscriptionPlans;

public class ActivateSubscriptionPlanTests
{
    private readonly Mock<IApplicationDbContext> _dbMock = new();
    private readonly List<SubscriptionPlan> _plans = [];

    private ActivateSubscriptionPlan.CommandHandler CreateHandler()
    {
        var plansSet = DbSetMockHelper.CreateMockDbSet(_plans);
        _dbMock.Setup(db => db.SubscriptionPlans).Returns(plansSet.Object);
        _dbMock
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return new ActivateSubscriptionPlan.CommandHandler(_dbMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPlanExistsAndIsInactive_ShouldActivatePlan()
    {
        // Arrange
        var plan = SubscriptionPlan.Create(
            "Starter",
            "starter",
            new Money(1000, "USD"),
            "month");
        plan.Deactivate();
        _plans.Add(plan);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ActivateSubscriptionPlan.Command(plan.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(plan.IsActive);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlanDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new ActivateSubscriptionPlan.Command(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SubscriptionPlan.NotFound", result.Error.Code);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
