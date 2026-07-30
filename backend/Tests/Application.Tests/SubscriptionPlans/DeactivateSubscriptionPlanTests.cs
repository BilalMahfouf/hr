using Application.Tests.Helpers;
using Moq;
using Shared.Abstracions;
using VeterinaryApi.Common.Abstracions;
using Shared.Domain.Common;
using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Features.SubscriptionPlans;

namespace Application.Tests.SubscriptionPlans;

public class DeactivateSubscriptionPlanTests
{
    private readonly Mock<IApplicationDbContext> _dbMock = new();
    private readonly List<SubscriptionPlan> _plans = [];

    private DeactivateSubscriptionPlan.CommandHandler CreateHandler()
    {
        var plansSet = DbSetMockHelper.CreateMockDbSet(_plans);
        _dbMock.Setup(db => db.SubscriptionPlans).Returns(plansSet.Object);
        _dbMock
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return new DeactivateSubscriptionPlan.CommandHandler(_dbMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPlanExistsAndIsActive_ShouldDeactivatePlan()
    {
        // Arrange
        var plan = SubscriptionPlan.Create(
            "Starter",
            "starter",
            new Money(1000, "USD"),
            "month");
        _plans.Add(plan);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new DeactivateSubscriptionPlan.Command(plan.Id), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(plan.IsActive);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlanDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new DeactivateSubscriptionPlan.Command(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SubscriptionPlan.NotFound", result.Error.Code);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
