using Application.Tests.Helpers;
using Moq;
using Modules.Shared.Abstracions;
using PublicApi.Common.Abstracions;
using Modules.Shared.Domain.Common;
using PublicApi.Domain.Common;
using PublicApi.Domain.Subscriptions;
using PublicApi.Features.SubscriptionPlans;

namespace Application.Tests.SubscriptionPlans;

public class GetSubscriptionPlanByIdTests
{
    private readonly Mock<IApplicationDbContext> _dbMock = new();
    private readonly List<SubscriptionPlan> _plans = [];

    private GetSubscriptionPlanById.QueryHandler CreateHandler()
    {
        var plansSet = DbSetMockHelper.CreateMockDbSet(_plans);
        _dbMock.Setup(db => db.SubscriptionPlans).Returns(plansSet.Object);
        return new GetSubscriptionPlanById.QueryHandler(_dbMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPlanExists_ShouldReturnMappedResponse()
    {
        // Arrange
        var plan = SubscriptionPlan.Create(
            "Starter",
            "starter",
            new Money(1500, "USD"),
            "month",
            2,
            7);
        _plans.Add(plan);

        var handler = CreateHandler();
        var query = new GetSubscriptionPlanById.Query(plan.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(plan.Id, result.Value.Id);
        Assert.Equal(plan.Name, result.Value.Name);
        Assert.Equal(plan.Slug, result.Value.Slug);
        Assert.Equal(plan.Price.Amount, result.Value.Amount);
        Assert.Equal(plan.Price.Currency, result.Value.Currency);
        Assert.Equal(plan.BillingInterval, result.Value.BillingInterval);
        Assert.Equal(plan.IntervalCount, result.Value.IntervalCount);
        Assert.Equal(plan.TrialDays, result.Value.TrialDays);
        Assert.Equal(plan.IsActive, result.Value.IsActive);
    }

    [Fact]
    public async Task Handle_WhenPlanDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetSubscriptionPlanById.Query(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SubscriptionPlan.NotFound", result.Error.Code);
    }
}
