
using Chargily.Pay;
using Chargily.Pay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reactive.Joins;
using Modules.Shared.Abstracions;
using PublicApi.Common.Abstracions;
using PublicApi.Common.Abstracions.Payments;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Domain.Subscriptions;
using PublicApi.Domain.Subscriptions.Errors;

namespace PublicApi.Features.Subscriptions.Endpoints;

public static class RenewSubscription
{
    public sealed record Command(
        Guid DoctorId,
        Guid PlanId,
        string IdempotencyKey) : ICommand<Shared.Response>;
    public sealed class CommandHandler(
        IApplicationDbContext db,
        IPaymentService paymentService)
        : ICommandHandler<Command, Shared.Response>
    {
        public async Task<Result<Shared.Response>> Handle(
            Command command,
            CancellationToken cancellationToken = default)
        {

            var hasActiveSubscription = await db.Subscriptions
                               .AnyAsync(s => s.DoctorId == command.DoctorId &&
                               (s.Status == SubscriptionStatus.Active ||
                               s.Status == SubscriptionStatus.Trialing),
                               cancellationToken);
            if (hasActiveSubscription)
            {
                return Result<Shared.Response>.Failure(SubscriptionErrors
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
                          .FirstOrDefaultAsync(
                          e => e.IdempotencyKey == command.IdempotencyKey
                          && e.Status == PaymentStatus.Pending,
                          cancellationToken);
            if (existingPayment?.ProviderPaymentId is not null)
            {

                var existingCheckoutResult = await paymentService
                    .GetCheckoutAsync(existingPayment.ProviderPaymentId);
                if (existingCheckoutResult.IsSuccess)
                {

                    return Result<Shared.Response>.Success(new Shared.Response(
                        existingCheckoutResult.Value.CheckoutUrl.ToString(),
                        null,
                       existingPayment.SubscriptionId));
                }

                return Result<Shared.Response>
                    .Failure(SubscriptionErrors
                            .FailedToRetrieveCheckout(existingPayment.Id));
            }


            var plan = await db.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == command.PlanId, cancellationToken);
            if (plan is null)
            {
                return Result<Shared.Response>.Failure(SubscriptionPlanErrors
                    .SubscriptionPlanNotFound(command.PlanId));
            }

            var oldSubscription = await db.Subscriptions
                .Where(e => e.DoctorId == command.DoctorId &&
                (e.Status == SubscriptionStatus.Expired ||
                e.Status == SubscriptionStatus.PastDue))
                .OrderByDescending(e => e.CurrentPeriodEnd)
                .Include(e => e.Plan)
                .FirstOrDefaultAsync(cancellationToken);
            if (oldSubscription == null)
            {
                return Result<Shared.Response>.Failure(SubscriptionErrors.NotFound);
            }
            var newSubscription = Subscription.Renew(oldSubscription, plan);
            db.Subscriptions.Add(newSubscription);

            var amount = new Domain.Common.Money(
                oldSubscription.Plan.Price.Amount,
                oldSubscription.Plan.Price.Currency);

            var payment = Payment.CreatePending(
                newSubscription.Id,
                newSubscription.DoctorId,
                amount,
                $"{nameof(ChargilyPay)}",
                command.IdempotencyKey);
            db.SubscriptionPayments.Add(payment);


            var metaData = new List<string>()
            {
             $"subscriptionId:{newSubscription.Id.ToString()}"
            };
            var checkoutResult = await paymentService
                .CreateCheckoutAsync(
                oldSubscription.Plan.Price.Amount,
                PublicApi.Common.Abstracions.Payments.Currency.DZD,
                payment.Id,
                metaData,
                cancellationToken);
            if (!checkoutResult.IsSuccess)
            {
                return Result<Shared.Response>
                    .Failure(SubscriptionErrors
                            .FailedToCreateCheckout);
            }
            payment.SetProviderPaymentId(checkoutResult.Value.ProviderPaymentId);
            await db.SaveChangesAsync(cancellationToken);

            var response = new Shared.Response(
                checkoutResult.Value.CheckoutUrl.ToString(),
                newSubscription.Status.ToString(),
                newSubscription.Id);
            return Result<Shared.Response>.Success(response);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/subscriptions/renew", async (
                ICurrentTenant tenant,
                [FromHeader(Name = $"{Shared.IdempotencyKeyHeader}")]
                  string idempotencyKey,
                [FromBody] Guid planId,
                ICommandHandler<Command, Shared.Response> handler,
                CancellationToken ct) =>
            {
                var command = new Command(tenant.UserId!.Value, planId, idempotencyKey);
                var result = await handler.Handle(command);
                return result.IsSuccess ? Results.Ok(result.Value)
                     : result.Problem();
            }).RequireAuthorization()
            .WithTags("Subscriptions")
            .WithSummary("Renew subscription")
            .WithDescription("Creates a renewal checkout for an expired or past-due subscription.")
            .Produces<Shared.Response>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("RenewSubscription");
        }
    }
}
