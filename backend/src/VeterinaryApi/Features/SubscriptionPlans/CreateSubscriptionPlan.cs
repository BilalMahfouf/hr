using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared.Abstracions;
using VeterinaryApi.Common.Abstracions;
using Shared.CQRS;
using Shared.Endpoints;
using Shared.Results;
using Shared.Domain.Common;
using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Domain.Subscriptions.Errors;

namespace VeterinaryApi.Features.SubscriptionPlans;

public static class CreateSubscriptionPlan
{
    public sealed record Command(
        string Name,
        decimal Amount,
        string Currency,
        string BillingInterval,
        int IntervalCount,
        int TrialDays) : ICommand<Response>;
    public sealed record Response(Guid Id);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(e => e.Name)
                .NotEmpty()
                .MaximumLength(100);
            RuleFor(e => e.Amount)
                .GreaterThanOrEqualTo(0);
            RuleFor(e => e.Currency)
                .NotEmpty()
                .Length(3);
            RuleFor(e => e.BillingInterval)
                .NotEmpty()
                .Must(e => new[] { "day", "week", "month", "year" }
                .Contains(e.ToLower()))
                .WithMessage("BillingInterval must be one of the following values: day, week, month, year.");
            RuleFor(e => e.IntervalCount)
                .GreaterThan(0);
            RuleFor(e => e.TrialDays)
                .GreaterThanOrEqualTo(0);
        }
    }
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

            var existingPlan = await db.SubscriptionPlans
                    .FirstOrDefaultAsync(
                    e => e.Name.ToLower() == command.Name.ToLower(),
                    cancellationToken);
            if (existingPlan is not null)
            {
                return Result<Response>.Failure(SubscriptionPlanErrors
                    .SubscriptionPlanNameNotUnique(command.Name));
            }

            var amount = new Money(command.Amount, command.Currency);
            var plan = SubscriptionPlan.Create(
                command.Name,
                command.Name,
                amount,
                command.BillingInterval,
                command.IntervalCount,
                command.TrialDays);

            db.SubscriptionPlans.Add(plan);
            await db.SaveChangesAsync(cancellationToken);

            return Result<Response>.Success(new Response(plan.Id));
        }
    }
    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("subscription-plans", async (
                Command command,
                ICommandHandler<Command, Response> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.Created(
                    $"/subscription-plans/{result.Value.Id}",
                    result.Value.Id)
                : result.Problem();
            }).RequireAuthorization()
            .WithTags($"{nameof(SubscriptionPlan)}s")
            .WithSummary("Create subscription plan")
            .WithDescription("Creates a new subscription plan with billing interval and trial configuration.")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("CreateSubscriptionPlan");
        }
    }
}
