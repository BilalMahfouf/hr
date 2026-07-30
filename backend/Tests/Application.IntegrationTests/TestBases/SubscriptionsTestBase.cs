using Application.IntegrationTests.Infrastructure;
using Chargily.Pay.Abstractions;
using Chargily.Pay.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Modules.Shared.Abstracions;
using PublicApi.Common.Abstracions;
using PublicApi.Common.Abstracions.Payments;
using Modules.Shared.Domain.Common;
using PublicApi.Domain.Common;
using PublicApi.Domain.Subscriptions;
using Modules.Identity.Domain.Users;
using PublicApi.Infrastructure.Payments;
using PublicApi.Infrastructure.Persistence;
using CreateSubscriptionEndpoint = PublicApi.Features.Subscriptions.Endpoints.CreateSubscirption;
using RenewSubscriptionEndpoint = PublicApi.Features.Subscriptions.Endpoints.RenewSubscription;
using SubscriptionCheckoutEndpoint = PublicApi.Features.Subscriptions.Endpoints.CreateCheckout;
using SubscriptionMeEndpoint = PublicApi.Features.Subscriptions.Endpoints.Me;

namespace Application.IntegrationTests.TestBases;

public abstract class SubscriptionsTestBase : IntegrationTestBase
{
    protected SubscriptionsTestBase(PostgresFixture fixture) : base(fixture)
    {
    }

    protected Mock<IChargilyPayClient> ChargilyClientMock { get; private set; } = null!;
    protected TestPaymentService PaymentService { get; private set; } = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        ChargilyClientMock = new Mock<IChargilyPayClient>(MockBehavior.Strict);
        services.AddSingleton(ChargilyClientMock.Object);

        services.AddOptions<ChargilyOptions>().Configure(options =>
        {
            options.ApiSecretKey = "test-secret";
            options.IsLiveMode = false;
            options.WebhookUrl = "https://example.test/webhook";
            options.SuccessUrl = "https://example.test/success";
            options.FailureUrl = "https://example.test/failure";
        });

        PaymentService = new TestPaymentService();
        services.AddSingleton<IPaymentService>(PaymentService);
    }

    protected SubscriptionCheckoutEndpoint.Handler CreateCreateCheckoutHandler(IServiceProvider services)
    {
        return new SubscriptionCheckoutEndpoint.Handler(
            services.GetRequiredService<IApplicationDbContext>(),
            services.GetRequiredService<IChargilyPayClient>(),
            services.GetRequiredService<IOptions<ChargilyOptions>>());
    }

    protected CreateSubscriptionEndpoint.CommandHandler CreateCreateSubscriptionHandler(IServiceProvider services)
    {
        return new CreateSubscriptionEndpoint.CommandHandler(
            services.GetRequiredService<IApplicationDbContext>(),
            services.GetRequiredService<FluentValidation.IValidator<CreateSubscriptionEndpoint.Command>>());
    }

    protected RenewSubscriptionEndpoint.CommandHandler CreateRenewSubscriptionHandler(IServiceProvider services)
    {
        return new RenewSubscriptionEndpoint.CommandHandler(
            services.GetRequiredService<IApplicationDbContext>(),
            services.GetRequiredService<IPaymentService>());
    }

    protected SubscriptionMeEndpoint.QueryHandler CreateSubscriptionMeHandler(IServiceProvider services)
    {
        return new SubscriptionMeEndpoint.QueryHandler(
            services.GetRequiredService<IApplicationDbContext>());
    }

    protected async Task<User> SeedDoctorAsync(
        ApplicationDbContext db,
        string email = "doctor@test.local",
        string password = "Pass1234!",
        string userName = "doctor",
        string firstName = "Doc",
        string lastName = "Tor")
    {
        var hasher = RootProvider.GetRequiredService<IPasswordHasher>();
        var user = User.Register(userName, firstName, lastName, email, hasher.Hash(password));
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    protected async Task<SubscriptionPlan> SeedPlanAsync(
        ApplicationDbContext db,
        string name = "Basic",
        string slug = "basic",
        decimal amount = 1500m,
        string interval = "month",
        int intervalCount = 1,
        int trialDays = 0)
    {
        var plan = SubscriptionPlan.Create(
            name,
            slug,
            Money.InDzd(amount),
            interval,
            intervalCount,
            trialDays);
        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    protected async Task<Subscription> SeedSubscriptionAsync(
        ApplicationDbContext db,
        Guid doctorId,
        SubscriptionPlan plan,
        SubscriptionStatus status)
    {
        var subscription = Subscription.Create(doctorId, plan);

        switch (status)
        {
            case SubscriptionStatus.Active:
                subscription.Activate();
                break;
            case SubscriptionStatus.PastDue:
                subscription.MarkPastDue();
                break;
            case SubscriptionStatus.Cancelled:
                subscription.Cancel();
                break;
            case SubscriptionStatus.Expired:
                subscription.MarkExpired();
                break;
            case SubscriptionStatus.PaymentFailed:
                subscription.PaymentFailed();
                break;
            case SubscriptionStatus.PaymentExpired:
                subscription.PaymentExipred();
                break;
            case SubscriptionStatus.Pending:
            case SubscriptionStatus.Trialing:
            default:
                break;
        }

        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync();
        return subscription;
    }

    protected async Task<Payment> SeedPaymentAsync(
        ApplicationDbContext db,
        Subscription subscription,
        Guid doctorId,
        string idempotencyKey,
        string? providerPaymentId = null,
        PaymentStatus status = PaymentStatus.Pending)
    {
        var payment = Payment.CreatePending(
            subscription.Id,
            doctorId,
            subscription.Plan.Price,
            "ChargilyPay",
            idempotencyKey);
        if (!string.IsNullOrWhiteSpace(providerPaymentId))
        {
            payment.SetProviderPaymentId(providerPaymentId);
        }
        switch (status)
        {
            case PaymentStatus.Paid:
                payment.MarkPaid();
                break;
            case PaymentStatus.Failed:
                payment.MarkFailed("failed");
                break;
            case PaymentStatus.Expired:
                payment.MarkExpired("expired");
                break;
            case PaymentStatus.Refunded:
                payment.MarkRefunded();
                break;
            case PaymentStatus.Pending:
            default:
                break;
        }

        db.SubscriptionPayments.Add(payment);
        await db.SaveChangesAsync();
        return payment;
    }

    protected static Response<CheckoutResponse> BuildCheckoutResponse(string id, Uri checkoutUrl)
    {
        return new Response<CheckoutResponse>
        {
            Id = id,
            Value = new CheckoutResponse
            {
                Id = id,
                CheckoutUrl = checkoutUrl
            }
        };
    }
}
