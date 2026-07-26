using Application.Tests.Helpers;
using FluentValidation;
using Moq;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Features.SubscriptionPlans;

namespace Application.Tests.SubscriptionPlans;

public class UpdateSubscriptionPlanTests
{
    private readonly Mock<IApplicationDbContext> _dbMock = new();
    private readonly List<SubscriptionPlan> _plans = [];

    private UpdateSubscriptionPlan.CommandHandler CreateHandler()
    {
        var plansSet = DbSetMockHelper.CreateMockDbSet(_plans);
        _dbMock.Setup(db => db.SubscriptionPlans).Returns(plansSet.Object);
        _dbMock
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new UpdateSubscriptionPlan.CommandHandler(
            _dbMock.Object,
            new UpdateSubscriptionPlan.Validator());
    }

    [Fact]
    public async Task Handle_WhenPlanExists_ShouldUpdateAndReturnSuccess()
    {
        // Arrange
        var plan = SubscriptionPlan.Create(
            "Starter",
            "starter",
            new Money(1000, "USD"),
            "month",
            1,
            0);
        _plans.Add(plan);

        var handler = CreateHandler();
        var command = new UpdateSubscriptionPlan.Command(
            plan.Id,
            "Pro",
            2500,
            "USD",
            "year",
            1,
            30);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(plan.Id, result.Value.Id);
        Assert.Equal("Pro", plan.Name);
        Assert.Equal(new Money(2500, "USD"), plan.Price);
        Assert.Equal("year", plan.BillingInterval);
        Assert.Equal(1, plan.IntervalCount);
        Assert.Equal(30, plan.TrialDays);

        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlanDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new UpdateSubscriptionPlan.Command(
            Guid.NewGuid(),
            "Pro",
            2500,
            "USD",
            "month",
            1,
            0);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SubscriptionPlan.NotFound", result.Error.Code);

        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCommandIsInvalid_ShouldThrowValidationException()
    {
        // Arrange
        var plan = SubscriptionPlan.Create(
            "Starter",
            "starter",
            new Money(1000, "USD"),
            "month");
        _plans.Add(plan);

        var handler = CreateHandler();
        var invalidCommand = new UpdateSubscriptionPlan.Command(
            Guid.Empty,
            "",
            -1,
            "US",
            "invalid",
            0,
            -1);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(invalidCommand, CancellationToken.None));

        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
