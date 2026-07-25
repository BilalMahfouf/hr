using Application.Tests.Helpers;
using FluentValidation;
using Moq;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Features.SubscriptionPlans;

namespace Application.Tests.SubscriptionPlans;

public class CreateSubscriptionPlanTests
{
    private readonly Mock<IApplicationDbContext> _dbMock = new();
    private readonly List<SubscriptionPlan> _plans = [];

    private CreateSubscriptionPlan.CommandHandler CreateHandler()
    {
        var plansSet = DbSetMockHelper.CreateMockDbSet(_plans);

        plansSet
            .Setup(s => s.Add(It.IsAny<SubscriptionPlan>()))
            .Callback<SubscriptionPlan>(plan => _plans.Add(plan));

        _dbMock.Setup(db => db.SubscriptionPlans).Returns(plansSet.Object);
        _dbMock
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new CreateSubscriptionPlan.CommandHandler(
            _dbMock.Object,
            new CreateSubscriptionPlan.Validator());
    }

    [Fact]
    public async Task Handle_WhenPlanNameIsUnique_ShouldCreatePlanAndReturnId()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new CreateSubscriptionPlan.Command(
            "Starter",
            1999,
            "USD",
            "month",
            1,
            14);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);

        var created = Assert.Single(_plans);
        Assert.Equal("Starter", created.Name);
        Assert.Equal("starter", created.Slug);
        Assert.Equal(new Money(1999, "USD"), created.Price);
        Assert.Equal("month", created.BillingInterval);
        Assert.Equal(1, created.IntervalCount);
        Assert.Equal(14, created.TrialDays);
        Assert.True(created.IsActive);

        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlanNameAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        _plans.Add(SubscriptionPlan.Create(
            "Starter",
            "starter",
            new Money(999, "USD"),
            "month"));

        var handler = CreateHandler();
        var command = new CreateSubscriptionPlan.Command(
            "starter",
            1999,
            "USD",
            "month",
            1,
            0);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SubscriptionPlan.AlreadyExists", result.Error.Code);
        Assert.Contains("starter", result.Error.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Single(_plans);

        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCommandIsInvalid_ShouldThrowValidationException()
    {
        // Arrange
        var handler = CreateHandler();
        var invalidCommand = new CreateSubscriptionPlan.Command(
            "",
            -1,
            "US",
            "invalid",
            0,
            -5);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(invalidCommand, CancellationToken.None));

        Assert.Empty(_plans);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
