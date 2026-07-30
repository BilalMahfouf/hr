
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Abstracions;
using VeterinaryApi.Common.Abstracions;
using Shared.CQRS;
using Shared.Endpoints;
using Shared.Results;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Domain.Subscriptions.Errors;

namespace VeterinaryApi.Features.Subscriptions.Endpoints;

public static class CreateSubscirption
{
    public sealed record Request(Guid PlanId);
    public sealed record Command(Guid DoctorId, Guid PlanId) : ICommand<Response>;
    public sealed record Response(Guid Id);

    public sealed class CommandHandler(
        IApplicationDbContext db,
        IValidator<Command> validator)
        : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(
            Command command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var haveExistingActiveSubscription = await db.Subscriptions
                .AnyAsync(
                e => e.DoctorId == command.DoctorId && e.Status != SubscriptionStatus.Cancelled,
                cancellationToken);
            if (haveExistingActiveSubscription)
            {
                return Result<Response>.Failure(SubscriptionErrors
                    .AlreadyExistAcitveSubscription);
            }

            var plan = await db.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == command.PlanId, cancellationToken);
            if (plan is null)
            {
                return Result<Response>.Failure(SubscriptionPlanErrors
                    .SubscriptionPlanNotFound(command.PlanId));
            }
            var subscription = Subscription.Create(
                command.DoctorId, plan);
            db.Subscriptions.Add(subscription);

            await db.SaveChangesAsync(cancellationToken);
            var response = new Response(subscription.Id);
            return Result<Response>.Success(response);
        }
    }
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DoctorId)
                .NotEmpty().WithMessage("DoctorId is required.");
            RuleFor(x => x.PlanId)
                .NotEmpty().WithMessage("PlanId is required.");
        }
    }

    //public sealed class Endpoint : IEndpoint
    //{
    //    public void AddRoutes(IEndpointRouteBuilder app)
    //    {
    //        app.MapPost("/subscriptions", async (
    //            [FromBody] Request request,
    //            [FromServices] ICurrentTenant tenant,
    //            ICommandHandler<Command, Response> handler,
    //            CancellationToken ct) =>
    //        {
    //            var command = new Command(tenant.UserId!.Value, request.PlanId);
    //            var result = await handler.Handle(command);
    //            return result.IsSuccess
    //                ? Results.Ok(result.Value)
    //                : result.Problem();
    //        }).RequireAuthorization()
    //        .WithTags($"{nameof(Subscriptions)}s")
    //        .WithDescription("Create a new subscription for a doctor.");
    //    }
    //}
}
