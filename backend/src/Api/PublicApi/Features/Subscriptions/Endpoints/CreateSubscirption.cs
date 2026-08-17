
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modules.Shared.Abstracions;
using PublicApi.Common.Abstracions;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Domain.Subscriptions;
using PublicApi.Domain.Subscriptions.Errors;

namespace PublicApi.Features.Subscriptions.Endpoints;

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
}
