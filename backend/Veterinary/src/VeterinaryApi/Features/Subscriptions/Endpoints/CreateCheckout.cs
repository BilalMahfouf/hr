using Chargily.Pay;
using Chargily.Pay.Abstractions;
using Chargily.Pay.Models;
using HandlebarsDotNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Resend;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Domain.Subscriptions.Errors;
using VeterinaryApi.Domain.Users;
using VeterinaryApi.Infrastructure.Payments;
using VeterinaryApi.Infrastructure.Persistence;

namespace VeterinaryApi.Features.Subscriptions.Endpoints;

public static class CreateCheckout
{
    public sealed record Request(Guid PlanId);
    public sealed record CreateSubscriptionCheckoutCommand(
    Guid DoctorId,
    Guid PlanId,
    string IdempotencyKey) : ICommand<Response>;

    public sealed record Response(
        string? CheckoutUrl,
        string? SubscriptionStatus,
        Guid SubscriptionId);

    public sealed class Handler(
        IApplicationDbContext db,
        IChargilyPayClient chargilyPayClient,
        IOptions<ChargilyOptions> options)
        : ICommandHandler<CreateSubscriptionCheckoutCommand, Response>
    {
        // 20/3/2026 todo: refactor this spaghetti handler since it do to much work
        public async Task<Result<Response>> Handle(
            CreateSubscriptionCheckoutCommand command,
            CancellationToken cancellationToken = default)
        {

            var hasActiveSubscription = await db.Subscriptions
                          .AnyAsync(s => s.DoctorId == command.DoctorId &&
                          (s.Status == SubscriptionStatus.Active ||
                          s.Status == SubscriptionStatus.Trialing),
                          cancellationToken);
            if (hasActiveSubscription)
            {
                return Result<Response>.Failure(SubscriptionErrors
                    .ActiveSubscriptionAlreadyExist);
            }

            var existingPayment = await db.SubscriptionPayments
                .Select(e => new
                {
                    e.Id,
                    e.ProviderPaymentId,
                    e.IdempotencyKey,
                    e.SubscriptionId,
                    e.Status
                })
                .FirstOrDefaultAsync(e => e.IdempotencyKey == command.IdempotencyKey &&
                e.Status == PaymentStatus.Pending,
                cancellationToken);
            if (existingPayment?.ProviderPaymentId is not null)
            {

                Response<CheckoutResponse>? existingCheckout = await chargilyPayClient
                    .GetCheckout(existingPayment.ProviderPaymentId);
                if (existingCheckout is null ||
                    existingCheckout.Value.CheckoutUrl is null)
                {
                    return Result<Response>.Failure(SubscriptionErrors
                        .FailedToRetrieveCheckout(existingPayment.Id));
                }

                return Result<Response>.Success(new Response(
                    existingCheckout.Value.CheckoutUrl.ToString(),
                    null,
                   existingPayment.SubscriptionId));
            }


            var isDoctorExist = await db.Users
                .AnyAsync(d => d.Id == command.DoctorId, cancellationToken);
            if (!isDoctorExist)
            {
                return Result<Response>.Failure(UserErrors.NotFound);
            }
            var plan = await db.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == command.PlanId, cancellationToken);
            if (plan is null)
            {
                return Result<Response>.Failure(SubscriptionPlanErrors
                    .SubscriptionPlanNotFound(command.PlanId));
            }

            var pendingSubscription = await db.Subscriptions
                                    .ForTenant(command.DoctorId)
                                    .Where(e => e.Status == SubscriptionStatus.Pending)
                                    .ToListAsync(cancellationToken);

            if (pendingSubscription.Any())
            {
                db.Subscriptions.RemoveRange(pendingSubscription);
                //await db.SaveChangesAsync(cancellationToken);
            }

            var subscription = Subscription.Create(command.DoctorId, plan);
            db.Subscriptions.Add(subscription);

            if (subscription.Status == SubscriptionStatus.Trialing)
            {
                await db.SaveChangesAsync(cancellationToken);
                return Result<Response>.Success(new Response(
                    null,
                    subscription.Status.ToString(),
                    subscription.Id));
            }

            var payment = Payment.CreatePending(
                subscription.Id,
                command.DoctorId,
                plan.Price,
                nameof(ChargilyPay),
                command.IdempotencyKey
                );
            db.SubscriptionPayments.Add(payment);


            var chargilyOptions = options.Value;

            var checkout = new Checkout(plan.Price.Amount, Currency.DZD)
            {
                Language = LocaleType.Arabic,
                PaymentMethod = PaymentMethod.EDAHABIA,
                PassFeesToCustomer = false,
                WebhookEndpointUrl = new Uri(chargilyOptions.WebhookUrl),
                OnFailureRedirectUrl = new Uri(chargilyOptions.FailureUrl),
                OnSuccessRedirectUrl = new Uri(chargilyOptions.SuccessUrl),
                CollectShippingAddress = false,
                Metadata = new List<string>
                {
                    $"paymentId:{payment.Id.ToString()}",
                    $"subscriptionId:{subscription.Id.ToString()}"
                },
            };
            var checkoutResult = await chargilyPayClient.CreateCheckout(checkout);
            if (checkoutResult is null || checkoutResult?.Id is null)
            {
                return Result<Response>.Failure(SubscriptionErrors
                    .FailedToRetrieveCheckout(Guid.Empty));
            }
            payment.SetProviderPaymentId(checkoutResult.Value.Id);

            await db.SaveChangesAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                checkoutResult.Value.CheckoutUrl!.ToString(),
                subscription.Status.ToString(),
                subscription.Id));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("subscriptions", async (
                [FromBody] Request request,
                [FromHeader(Name = $"{Shared.IdempotencyKeyHeader}")] string idempotencyKey,
                ICurrentTenant currentTenant,
                ICommandHandler<CreateSubscriptionCheckoutCommand, Response> handler,
                CancellationToken ct
                ) =>
            {
                var command = new CreateSubscriptionCheckoutCommand(
                    currentTenant.UserId!.Value,
                    request.PlanId,
                    idempotencyKey
                    );
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.Ok(result.Value)
                    : result.Problem();

            }).RequireAuthorization()
            .WithTags($"{nameof(Subscription)}s")
            .WithSummary("Create subscription checkout")
            .WithDescription("Creates a new subscription checkout for the current authenticated doctor and returns the checkout URL or trialing status.")
            .Produces<Response>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("CreateSubscriptionCheckout");
        }
    }
}
