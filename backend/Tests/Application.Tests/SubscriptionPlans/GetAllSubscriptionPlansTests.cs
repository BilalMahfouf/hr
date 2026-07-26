using Application.Tests.Helpers;
using Moq;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Features.SubscriptionPlans;

namespace Application.Tests.SubscriptionPlans;

public class GetAllSubscriptionPlansTests
{
    private readonly Mock<IApplicationDbContext> _dbMock = new();
    private readonly List<SubscriptionPlan> _plans = [];

    private GetAllSubscriptionPlans.QueryHandler CreateHandler()
    {
        var plansSet = DbSetMockHelper.CreateMockDbSet(_plans);
        _dbMock.Setup(db => db.SubscriptionPlans).Returns(plansSet.Object);
        return new GetAllSubscriptionPlans.QueryHandler(_dbMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPlansExist_ShouldReturnAllMappedPlans()
    {
        // Arrange
        _plans.Add(SubscriptionPlan.Create(
            "Starter",
            "starter",
            new Money(1000, "USD"),
            "month"));
        _plans.Add(SubscriptionPlan.Create(
            "Pro",
            "pro",
            new Money(5000, "USD"),
            "year"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAllSubscriptionPlans.Query(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var plans = result.Value.ToList();
        Assert.Equal(2, plans.Count);
        Assert.Contains(plans, p => p.Name == "Starter" && p.BillingInterval == "month");
        Assert.Contains(plans, p => p.Name == "Pro" && p.BillingInterval == "year");
    }

    [Fact]
    public async Task Handle_WhenNoPlansExist_ShouldReturnNotFound()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAllSubscriptionPlans.Query(), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SubscriptionPlan.NotFound", result.Error.Code);
    }
}
