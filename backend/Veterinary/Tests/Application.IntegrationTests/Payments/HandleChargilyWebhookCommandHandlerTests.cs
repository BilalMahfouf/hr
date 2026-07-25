using System.Text.Json;
using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Features.Subscriptions.Webhooks;
using VeterinaryApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Payments;

public sealed class HandleChargilyWebhookCommandHandlerTests : PaymentsTestBase
{
    public HandleChargilyWebhookCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenBodyMissing_ReturnsFailure()
    {
        using var scope = CreateScope();
        var handler = CreateWebhookHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new HandleChargilyWebhook.Command(string.Empty),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Chargily.BodyNull", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenPayloadNull_ReturnsFailure()
    {
        using var scope = CreateScope();
        var handler = CreateWebhookHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new HandleChargilyWebhook.Command("null"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Chargily.PayloadNull", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenPaid_UpdatesPaymentAndSubscription()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db);
        var subscription = await SeedSubscriptionAsync(db, doctor.Id, plan, SubscriptionStatus.Pending);
        var payment = await SeedPaymentAsync(db, subscription, doctor.Id, "key-1", status: PaymentStatus.Pending);

        var payload = new HandleChargilyWebhook.ChargilyWebhookPayload
        {
            Type = "checkout.paid",
            Data = new HandleChargilyWebhook.ChargilyCheckoutData
            {
                Id = "chk_1",
                Status = "paid",
                Metadata = new List<string> { $"paymentId:{payment.Id}" }
            }
        };
        var json = JsonSerializer.Serialize(payload);

        var handler = CreateWebhookHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new HandleChargilyWebhook.Command(json),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedPayment = await db.SubscriptionPayments.SingleAsync(p => p.Id == payment.Id);
        Assert.Equal(PaymentStatus.Paid, updatedPayment.Status);

        var updatedSubscription = await db.Subscriptions.SingleAsync(s => s.Id == subscription.Id);
        Assert.Equal(SubscriptionStatus.Active, updatedSubscription.Status);
    }

    [Fact]
    public async Task Handle_WhenFailed_UpdatesPaymentAndSubscription()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db);
        var subscription = await SeedSubscriptionAsync(db, doctor.Id, plan, SubscriptionStatus.Pending);
        var payment = await SeedPaymentAsync(db, subscription, doctor.Id, "key-1", status: PaymentStatus.Pending);

        var payload = new HandleChargilyWebhook.ChargilyWebhookPayload
        {
            Type = "checkout.failed",
            Data = new HandleChargilyWebhook.ChargilyCheckoutData
            {
                Id = "chk_2",
                Status = "failed",
                FailureReason = "failed",
                Metadata = new List<string> { $"paymentId:{payment.Id}" }
            }
        };
        var json = JsonSerializer.Serialize(payload);

        var handler = CreateWebhookHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new HandleChargilyWebhook.Command(json),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updatedPayment = await db.SubscriptionPayments.SingleAsync(p => p.Id == payment.Id);
        Assert.Equal(PaymentStatus.Failed, updatedPayment.Status);
        Assert.Equal("failed", updatedPayment.FailureReason);

        var updatedSubscription = await db.Subscriptions.SingleAsync(s => s.Id == subscription.Id);
        Assert.Equal(SubscriptionStatus.PaymentFailed, updatedSubscription.Status);
    }

    [Fact]
    public async Task Handle_WhenMetadataMissing_ReturnsSuccess()
    {
        using var scope = CreateScope();
        var handler = CreateWebhookHandler(scope.ServiceProvider);

        var payload = new HandleChargilyWebhook.ChargilyWebhookPayload
        {
            Type = "checkout.paid",
            Data = new HandleChargilyWebhook.ChargilyCheckoutData
            {
                Id = "chk_3",
                Status = "paid",
                Metadata = null
            }
        };
        var json = JsonSerializer.Serialize(payload);

        var result = await handler.Handle(
            new HandleChargilyWebhook.Command(json),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
